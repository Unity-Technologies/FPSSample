using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;

using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;

namespace  UnityEditor.VFX.UI
{
    class VFXBlackboard : Blackboard, IControlledElement<VFXViewController>, IVFXMovable
    {
        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    Clear();
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }

        new void Clear()
        {
            m_DefaultCategory.Clear();

            foreach (var cat in m_Categories)
            {
                cat.Value.RemoveFromHierarchy();
            }
            m_Categories.Clear();
        }

        VFXView m_View;

        public VFXBlackboard(VFXView view)
        {
            m_View = view;
            editTextRequested = OnEditName;
            addItemRequested = OnAddItem;

            this.scrollable = true;

            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.blackboard, defaultRect));

            m_DefaultCategory = new VFXBlackboardCategory() { title = "parameters"};
            Add(m_DefaultCategory);
            m_DefaultCategory.headerVisible = false;

            AddStyleSheetPath("VFXBlackboard");

            RegisterCallback<MouseDownEvent>(OnMouseClick, TrickleDown.TrickleDown);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            focusIndex = 0;


            m_DragIndicator = new VisualElement();

            m_DragIndicator.name = "dragIndicator";
            m_DragIndicator.style.positionType = PositionType.Absolute;
            shadow.Add(m_DragIndicator);

            clippingOptions = ClippingOptions.ClipContents;
            SetDragIndicatorVisible(false);

            Resizer resizer = this.Query<Resizer>();

            shadow.Add(new ResizableElement());

            style.positionType = PositionType.Absolute;

            subTitle = "Parameters";

            resizer.RemoveFromHierarchy();
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.F2)
            {
                var graphView = GetFirstAncestorOfType<VFXView>();

                var field = graphView.selection.OfType<VFXBlackboardField>().FirstOrDefault();
                if (field != null)
                {
                    field.OpenTextEditor();
                }
                else
                {
                    var category = graphView.selection.OfType<VFXBlackboardCategory>().FirstOrDefault();

                    if (category != null)
                    {
                        category.OpenTextEditor();
                    }
                }
            }
        }

        private void SetDragIndicatorVisible(bool visible)
        {
            if (visible && (m_DragIndicator.parent == null))
            {
                shadow.Add(m_DragIndicator);
                m_DragIndicator.visible = true;
            }
            else if ((visible == false) && (m_DragIndicator.parent != null))
            {
                shadow.Remove(m_DragIndicator);
            }
        }

        VisualElement m_DragIndicator;


        int InsertionIndex(Vector2 pos)
        {
            VisualElement owner = contentContainer != null ? contentContainer : this;
            Vector2 localPos = this.ChangeCoordinatesTo(owner, pos);

            if (owner.ContainsPoint(localPos))
            {
                int defaultCatIndex = IndexOf(m_DefaultCategory);

                for (int i = defaultCatIndex + 1; i < childCount; ++i)
                {
                    VFXBlackboardCategory cat = ElementAt(i) as VFXBlackboardCategory;
                    if (cat == null)
                    {
                        return i;
                    }

                    Rect rect = cat.layout;

                    if (localPos.y <= (rect.y + rect.height / 2))
                    {
                        return i;
                    }
                }
                return childCount;
            }
            return -1;
        }

        int m_InsertIndex;

        void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            if (selection == null)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            if (selection.Any(t => !(t is VFXBlackboardCategory)))
            {
                SetDragIndicatorVisible(false);
                return;
            }

            Vector2 localPosition = e.localMousePosition;

            m_InsertIndex = InsertionIndex(localPosition);

            if (m_InsertIndex != -1)
            {
                float indicatorY = 0;

                if (m_InsertIndex == childCount)
                {
                    if (childCount > 0)
                    {
                        VisualElement lastChild = this[childCount - 1];

                        indicatorY = lastChild.ChangeCoordinatesTo(this, new Vector2(0, lastChild.layout.height + lastChild.style.marginBottom)).y;
                    }
                    else
                    {
                        indicatorY = this.contentRect.height;
                    }
                }
                else
                {
                    VisualElement childAtInsertIndex = this[m_InsertIndex];

                    indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this, new Vector2(0, -childAtInsertIndex.style.marginTop)).y;
                }

                SetDragIndicatorVisible(true);

                m_DragIndicator.style.positionTop =  indicatorY - m_DragIndicator.style.height * 0.5f;

                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
            else
            {
                SetDragIndicatorVisible(false);
            }
            e.StopPropagation();
        }

        public int GetCategoryIndex(VFXBlackboardCategory cat)
        {
            return IndexOf(cat) - IndexOf(m_DefaultCategory) - 1;
        }

        void OnDragPerformEvent(DragPerformEvent e)
        {
            SetDragIndicatorVisible(false);
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            if (selection == null)
            {
                return;
            }

            var category = selection.OfType<VFXBlackboardCategory>().FirstOrDefault();
            if (category == null)
            {
                return;
            }

            if (m_InsertIndex != -1)
            {
                if (m_InsertIndex > IndexOf(category))
                    --m_InsertIndex;
                controller.MoveCategory(category.title, m_InsertIndex - IndexOf(m_DefaultCategory) - 1);
            }

            SetDragIndicatorVisible(false);
            e.StopPropagation();
        }

        void OnDragLeaveEvent(DragLeaveEvent e)
        {
            SetDragIndicatorVisible(false);
        }

        public void ValidatePosition()
        {
            BoardPreferenceHelper.ValidatePosition(this, m_View, defaultRect);
        }

        static readonly Rect defaultRect = new Rect(100, 100, 300, 500);

        void OnMouseClick(MouseDownEvent e)
        {
            m_View.SetBoardToFront(this);
        }

        void OnAddParameter(object parameter)
        {
            var selectedCategory = m_View.selection.OfType<VFXBlackboardCategory>().FirstOrDefault();
            VFXParameter newParam = m_Controller.AddVFXParameter(Vector2.zero, (VFXModelDescriptorParameters)parameter);
            if (selectedCategory != null && newParam != null)
                newParam.category = selectedCategory.title;
        }

        void OnAddItem(Blackboard bb)
        {
            GenericMenu menu = new GenericMenu();


            menu.AddItem(EditorGUIUtility.TrTextContent("Category"), false, OnAddCategory);
            menu.AddSeparator(string.Empty);

            foreach (var parameter in VFXLibrary.GetParameters())
            {
                VFXParameter model = parameter.model as VFXParameter;

                var type = model.type;
                if (type == typeof(GPUEvent))
                    continue;

                menu.AddItem(EditorGUIUtility.TextContent(type.UserFriendlyName()), false, OnAddParameter, parameter);
            }

            menu.ShowAsContext();
        }

        public void SetCategoryName(VFXBlackboardCategory cat, string newName)
        {
            int index = GetCategoryIndex(cat);

            bool succeeded = controller.SetCategoryName(index, newName);

            if (succeeded)
            {
                m_Categories.Remove(cat.title);
                cat.title = newName;
                m_Categories.Add(newName, cat);
            }
        }

        void OnAddCategory()
        {
            string newCategoryName = EditorGUIUtility.TrTextContent("new category").text;
            int cpt = 1;
            while (controller.graph.UIInfos.categories.Any(t => t.name == newCategoryName))
            {
                newCategoryName = string.Format(EditorGUIUtility.TrTextContent("new category {0}").text, cpt++);
            }

            controller.graph.UIInfos.categories.Add(new VFXUI.CategoryInfo() { name = newCategoryName });
            controller.graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        void OnEditName(Blackboard bb, VisualElement element, string value)
        {
            if (element is VFXBlackboardField)
            {
                (element as VFXBlackboardField).controller.exposedName = value;
            }
        }

        public void OnMoveParameter(IEnumerable<VFXBlackboardRow> rows, VFXBlackboardCategory category, int index)
        {
            //TODO sort elements
            foreach (var row in rows)
            {
                controller.SetParametersOrder(row.controller, index++, category == m_DefaultCategory ? "" : category.title);
            }
        }

        public void SetCategoryExpanded(VFXBlackboardCategory category, bool expanded)
        {
            controller.SetCategoryExpanded(category.title, expanded);
        }

        VFXBlackboardCategory m_DefaultCategory;
        Dictionary<string, VFXBlackboardCategory> m_Categories = new Dictionary<string, VFXBlackboardCategory>();


        public VFXBlackboardRow GetRowFromController(VFXParameterController controller)
        {
            VFXBlackboardCategory cat = null;
            VFXBlackboardRow row = null;
            if (string.IsNullOrEmpty(controller.model.category))
            {
                row = m_DefaultCategory.GetRowFromController(controller);
            }
            else if (m_Categories.TryGetValue(controller.model.category, out cat))
            {
                row = cat.GetRowFromController(controller);
            }

            return row;
        }

        Dictionary<string, bool> m_ExpandedStatus = new Dictionary<string, bool>();
        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller || e.controller is VFXParameterController) //optim : reorder only is only the order has changed
            {
                if (e.controller == controller && e.change == VFXViewController.Change.assetName)
                {
                    title = controller.name;
                    return;
                }

                var orderedCategories = controller.graph.UIInfos.categories;
                var newCategories = new List<VFXBlackboardCategory>();

                if (orderedCategories != null)
                {
                    foreach (var catModel in controller.graph.UIInfos.categories)
                    {
                        VFXBlackboardCategory cat = null;
                        if (!m_Categories.TryGetValue(catModel.name, out cat))
                        {
                            cat = new VFXBlackboardCategory() {title = catModel.name };
                            cat.SetSelectable();
                            m_Categories.Add(catModel.name, cat);
                        }
                        m_ExpandedStatus[catModel.name] = !catModel.collapsed;

                        newCategories.Add(cat);
                    }

                    foreach (var category in m_Categories.Keys.Except(orderedCategories.Select(t => t.name)).ToArray())
                    {
                        m_Categories[category].RemoveFromHierarchy();
                        m_Categories.Remove(category);
                        m_ExpandedStatus.Remove(category);
                    }
                }

                var prevCat = m_DefaultCategory;

                foreach (var cat in newCategories)
                {
                    if (cat.parent == null)
                        Insert(IndexOf(prevCat) + 1, cat);
                    else
                        cat.PlaceInFront(prevCat);
                    prevCat = cat;
                }

                var actualControllers = new HashSet<VFXParameterController>(controller.parameterControllers.Where(t => string.IsNullOrEmpty(t.model.category)));
                m_DefaultCategory.SyncParameters(actualControllers);


                foreach (var cat in newCategories)
                {
                    actualControllers = new HashSet<VFXParameterController>(controller.parameterControllers.Where(t => t.model.category == cat.title));
                    cat.SyncParameters(actualControllers);
                    cat.expanded = m_ExpandedStatus[cat.title];
                }
            }
        }

        public override void UpdatePresenterPosition()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.blackboard, GetPosition());
        }

        public void OnMoved()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.blackboard, GetPosition());
        }
    }
}

namespace UnityEngine.Experimental.UIElements
{
    public class DownClickable : MouseManipulator
    {
        public event System.Action clicked;

        // Click-once type constructor
        public DownClickable(System.Action handler)
        {
            clicked = handler;

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (clicked != null)
            {
                clicked();
                evt.StopPropagation();
            }
        }
    }
}

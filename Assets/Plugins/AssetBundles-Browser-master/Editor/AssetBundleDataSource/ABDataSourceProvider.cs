using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AssetBundleBrowser.AssetBundleDataSource
{
    internal class ABDataSourceProviderUtility {

        private static List<Type> s_customNodes;

        internal static List<Type> CustomABDataSourceTypes {
            get {
                if(s_customNodes == null) {
                    s_customNodes = BuildCustomABDataSourceList();
                }
                return s_customNodes;
            }
        }

        private static List<Type> BuildCustomABDataSourceList()
        {
            var properList = new List<Type>();
            properList.Add(null); //empty spot for "default" 
            var x = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in x)
            {
                try
                {
                    var list = new List<Type>(
                        assembly
                        .GetTypes()
                        .Where(t => t != typeof(ABDataSource))
                        .Where(t => typeof(ABDataSource).IsAssignableFrom(t)));


                    for (int count = 0; count < list.Count; count++)
                    {
                        if (list[count].Name == "AssetDatabaseABDataSource")
                            properList[0] = list[count];
                        else if (list[count] != null)
                            properList.Add(list[count]);
                    }
                }
                catch (System.Exception)
                {
                    //assembly which raises exception on the GetTypes() call - ignore it
                }
            }


            return properList;
        }
    }
}

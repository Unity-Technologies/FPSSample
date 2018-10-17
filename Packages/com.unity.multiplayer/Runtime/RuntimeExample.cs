// -----------------------------------------------------------------------------
//
// Use this runtime example C# file to develop runtime code.
//
// -----------------------------------------------------------------------------

namespace UnityEngine.SubGroup.YourPackageName
{
    /// <summary>
    /// Provide a general description of the public class.
    /// </summary>
    /// <remarks>
    /// Packages require XmlDoc documentation for ALL Package APIs.
    /// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/xml-documentation-comments
    /// </remarks>
    public class MyPublicRuntimeExampleClass
    {
        /// <summary>
        /// Provide a description of what this private method does.
        /// </summary>
        /// <param name="parameter1"> Description of parameter 1 </param>
        /// <param name="parameter2"> Description of parameter 2 </param>
        /// <param name="parameter3"> Description of parameter 3 </param>
        /// <returns> Description of what the function returns </returns>
        public int CountThingsAndDoStuff(int parameter1, int parameter2, bool parameter3)
        {
            return parameter3 ? (parameter1 + parameter2) : (parameter1 - parameter2);
        }
    }
}
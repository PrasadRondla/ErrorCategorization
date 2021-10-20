using System;

namespace ErrorCategorizationTool
{
    class Program
    {
        static void Main(string[] args)
        {
            clsMain clsObj = new clsMain();
            clsObj.ReadJenkinsFailureResults();
           
        }
    }
}

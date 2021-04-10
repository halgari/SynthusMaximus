using System;

namespace SynthusMaximus.Support.RunSorting
{
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RunAfterAttribute : Attribute
    {
        public RunAfterAttribute(Type runAfter)
        {
            RunAfter = runAfter;
        }

        public Type RunAfter { get; set; }
    }
    

}
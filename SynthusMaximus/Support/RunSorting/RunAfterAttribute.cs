using System;

namespace SynthusMaximus.Support.RunSorting
{
    
    [AttributeUsage(AttributeTargets.Class)]
    public class RunAfterAttribute : Attribute
    {
        public RunAfterAttribute(Type runAfter)
        {
            RunAfter = runAfter;
        }

        public Type RunAfter { get; set; }
    }
    

}
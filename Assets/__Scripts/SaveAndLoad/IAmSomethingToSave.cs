using System;

namespace SaveAndLoad
{
    public interface IAmSomethingToSave
    {
        public IAmSomethingToSave SetDefault();
        public IAmSomethingToSave Init();
    }
}
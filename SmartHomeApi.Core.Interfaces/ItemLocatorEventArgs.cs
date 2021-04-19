using System;

namespace SmartHomeApi.Core.Interfaces
{
    public class ItemLocatorEventArgs : EventArgs
    {
        public string ItemType { get; set; }
    }
}
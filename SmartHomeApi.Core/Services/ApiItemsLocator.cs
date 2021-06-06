using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class ApiItemsLocator : IApiItemsLocator
    {
        public bool IsInitialized { get; private set; }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            throw new NotImplementedException();
        }

        public async Task Initialize()
        {
            //Get items from locators and initialize them

            IsInitialized = true;
        }
    }
}

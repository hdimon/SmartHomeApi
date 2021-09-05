using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class NotificationsProcessor : INotificationsProcessor
    {
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private readonly List<IStateChangedSubscriber> _stateChangedSubscribers = new List<IStateChangedSubscriber>();
        private readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public string ItemType => null;
        public string ItemId => null;

        public NotificationsProcessor(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _logger = fabric.GetApiLogger();
        }

        public void RegisterSubscriber(IStateChangedSubscriber subscriber)
        {
            try
            {
                RwLock.EnterWriteLock();

                _stateChangedSubscribers.Add(subscriber);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            finally
            {
                RwLock.ExitWriteLock();
            }
        }

        public void UnregisterSubscriber(IStateChangedSubscriber subscriber)
        {
            try
            {
                RwLock.EnterWriteLock();

                _stateChangedSubscribers.Remove(subscriber);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            finally
            {
                RwLock.ExitWriteLock();
            }
        }

        public void NotifySubscribers(StateChangedEvent args)
        {
            var untrackedItems = _fabric.GetConfiguration().UntrackedItems;

            var untrackedItem = untrackedItems.FirstOrDefault(i => i.ItemId == args.ItemId);

            if (untrackedItem != null)
            {
                if (!untrackedItem.ApplyOnlyEnumeratedStates) //It means item is not tracked at all
                    return;

                if (untrackedItem.States != null && untrackedItem.States.Any(itemId => itemId == args.ItemId))
                    return;
            }

            if (args.EventType != StateChangedEventType.ValueSet) //All ValueSet notifications should be sent
            {
                var areEqual = ObjectsAreEqual(args.OldValue, args.NewValue);

                if (areEqual)
                    return;
            }

            try
            {
                RwLock.EnterReadLock();

                foreach (var stateChangedSubscriber in _stateChangedSubscribers)
                {
                    stateChangedSubscriber.Notify(args).ContinueWith(t => { _logger.Error(t.Exception); },
                        TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            finally
            {
                RwLock.ExitReadLock();
            }
        }

        private bool ObjectsAreEqual(object obj1, object obj2)
        {
            //TODO test this method. Maybe it's faster just to serialize objects.
            //If one of values is null but another not then they are not equal
            if (obj1 == null && obj2 != null || obj1 != null && obj2 == null)
                return false;

            //If both are null then they are equal
            if (obj1 == null)
                return true;

            var type = obj1.GetType();

            if (type != obj2.GetType())
                return false;

            //TODO change that to working with any reference type but not only dictionary
            var obj1IsDict = TypeHelper.IsDictionary(obj1);
            var obj2IsDict = TypeHelper.IsDictionary(obj2);

            if (obj1IsDict && obj2IsDict)
            {
                var obj1String = JsonConvert.SerializeObject(obj1);
                var obj2String = JsonConvert.SerializeObject(obj2);

                return obj1String == obj2String;
            }

            if (obj1IsDict || obj2IsDict)
                return false;
            
            var comparer = new ObjectsComparer.Comparer();

            var isEqual = comparer.Compare(type, obj1, obj2);

            return isEqual;
        }
    }
}
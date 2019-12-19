using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStatesContainerTransformer
    {
        void AddStateChangedEvent(StateChangedEvent ev);
        void Transform(IStatesContainer state, List<IStateTransformable> transformables);
        void RemoveStateChangedEvent(StateChangedEvent ev);
        bool ParameterIsTransformed(string deviceId, string parameter);
        bool TransformationIsNeeded();
    }
}
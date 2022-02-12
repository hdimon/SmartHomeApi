namespace SmartHomeApi.Core.Interfaces;

public interface IObjectToDynamicConverter
{
    dynamic Convert(object source);
}
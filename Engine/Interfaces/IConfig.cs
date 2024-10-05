using Engine.Models;

namespace Engine.Interfaces;

public interface IConfig
{
    Configuration? LoadConfig();
}

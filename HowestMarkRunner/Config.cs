using System.Collections.Generic;

namespace HowestMarkRunner
{
    public class Config
    {
        public IList<ExecutableConfig> Executables { get; set; }
        public IList<string> Tests { get; set; }
    }
}

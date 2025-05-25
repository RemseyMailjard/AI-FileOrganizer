using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_FileOrganizer.Utils
{
    /// <summary>
    /// Een eenvoudige logging interface zodat je makkelijk van logger kunt wisselen.
    /// </summary>
    public interface ILogger
    {
        void Log(string message);
    }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{
    internal class BD_MUTEX
    {
        private Mutex mutex;

        public BD_MUTEX() {
            mutex = new Mutex();
        }

        public int bd_mutex_destroy()
        {
            this.mutex = null;
            return 0;
        }

        public int bd_mutex_lock()
        {
            this.mutex.WaitOne();
            return 0;
        }

        public int bd_mutex_unlock()
        {
            this.mutex.ReleaseMutex();
            return 0;
        }
    }
}

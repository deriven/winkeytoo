using System;
using System.Diagnostics;

namespace WinKeyToo.Instrumentation
{
    internal class Tracing : IDisposable
    {
        private static readonly TraceSwitch DefaultSwitch = new TraceSwitch("DefaultSwitch", "Switch in the config file", "4");
        // Track whether Dispose has been called.
        private bool disposed;
        private readonly bool writeEnterExit;

        public Tracing()
        {
            writeEnterExit = true;
            Write(DefaultSwitch.TraceVerbose, 2, "Entering");
        }

        public Tracing(bool writeEnterExit)
        {
            this.writeEnterExit = writeEnterExit;
            Write(DefaultSwitch.TraceVerbose && writeEnterExit, 2, "Entering");
        }

        public void WriteError(params object[] data)
        {
            Write(DefaultSwitch.TraceError, 2, data);
        }

        public void WriteWarning(params object[] data)
        {
            Write(DefaultSwitch.TraceWarning, 2, data);
        }

        public void WriteInfo(params object[] data)
        {
            Write(DefaultSwitch.TraceInfo, 2, data);
        }

        public void WriteVerbose(params object[] data)
        {
            Write(DefaultSwitch.TraceVerbose, 2, data);
        }

        [Conditional("DEBUG")]
        private static void Write(bool shouldWrite, int level, params object[] data)
        {
            if (!shouldWrite) return;
            var stackTrace = new StackTrace();
            var stackFrame = stackTrace.GetFrame(level);
            var methodBase = stackFrame.GetMethod();
            var prefix = string.Format("[{0}.{1}(", methodBase.DeclaringType.FullName, methodBase.Name);
            var parameters = methodBase.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0) prefix += ",";
                prefix += string.Format("{0}", parameters[i].Name);
            }
            prefix += ")]";

            if (data != null && data.Length > 0)
            {
                for (var i = 0; i < data.Length; i++)
                {
                    if (data[i] is Exception)
                    {
                        Trace.WriteLine(string.Format("{0} {1}", prefix, data[i].GetType().FullName));
                        var curEx = data[i] as Exception;
                        var indent = 0;
                        while (curEx != null)
                        {
                            Trace.WriteLine(string.Format("{0}", curEx));
                            Trace.Indent();
                            curEx = curEx.InnerException;
                            indent++;
                        }
                        while (indent-- > 0) Trace.Unindent();
                    }
                    else
                    {
                        Trace.WriteLine(string.Format("{0} {1}", prefix, data[i]));
                    }
                }
            }
            else
            {
                Trace.WriteLine(prefix);
            }
        }

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (disposed) return;
            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if (disposing)
            {
                // Dispose managed resources.
            }

            // Call the appropriate methods to clean up
            // unmanaged resources here.
            // If disposing is false,
            // only the following code is executed.

            Write(DefaultSwitch.TraceVerbose && writeEnterExit, 2, "Exiting");

            // Note disposing has been done.
            disposed = true;
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~Tracing()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }
    }
}

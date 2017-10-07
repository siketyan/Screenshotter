using System;

namespace Screenshotter.Utilities
{
    public static class ExceptionUtil
    {
        public static string ToDetailString(this Exception ex)
        {
            var str = $"{ex.GetType()}: {ex.Message}\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                str += "\nCaused by: " + ToDetailString(ex.InnerException);
            }

            return str;
        }
    }
}
using System;

namespace AzFunc.IsolatedProcess.Helpers
{
    public static class ByteSize
    {
        private const long BytesInKilobyte = 1024;
        private const long BytesInMegabyte = BytesInKilobyte * 1024;
        private const long BytesInGigabyte = BytesInMegabyte * 1024;
        private const long BytesInTerabyte = BytesInGigabyte * 1024;

        public static long FromKilobytes(double kilobytes) => (long)(kilobytes * BytesInKilobyte);
        public static long FromMegabytes(double megabytes) => (long)(megabytes * BytesInMegabyte);
        public static long FromGigabytes(double gigabytes) => (long)(gigabytes * BytesInGigabyte);
        public static long FromTerabytes(double terabytes) => (long)(terabytes * BytesInTerabyte);

        public static double ToKilobytes(long bytes) => bytes / (double)BytesInKilobyte;
        public static double ToMegabytes(long bytes) => bytes / (double)BytesInMegabyte;
        public static double ToGigabytes(long bytes) => bytes / (double)BytesInGigabyte;
        public static double ToTerabytes(long bytes) => bytes / (double)BytesInTerabyte;
    }
}

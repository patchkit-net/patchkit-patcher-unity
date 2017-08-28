#include "checkdiskspace.hpp"

#include <sys/statvfs.h>

bool getAvailableDiskSpace(const char* t_path, long* out_freeBytes)
{
    struct statvfs stat;
    
    if (statvfs(t_path, &stat) == 0)
    {
        *out_freeBytes = (long)(stat.f_bavail * stat.f_frsize);
        return true;
    }
    else
    {
        return false;
    }
}

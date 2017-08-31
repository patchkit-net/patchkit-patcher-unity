#pragma once

extern "C" {
    bool getAvailableDiskSpace(const char* t_path, long* out_freeBytes);
}

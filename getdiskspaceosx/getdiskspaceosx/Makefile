CXX=g++
OPTIMIZATION=O2

build: libgetdiskspace_x86.so libgetdiskspace_x86_64.so

libgetdiskspace_x86.so: checkdiskspace.cpp
	$(CXX) -$(OPTIMIZATION) -m32 -shared -fPIC checkdiskspace.cpp -o libgetdiskspace_x86.so

libgetdiskspace_x86_64.so: checkdiskspace.cpp
	$(CXX) -$(OPTIMIZATION) -m64 -shared -fPIC checkdiskspace.cpp -o libgetdiskspace_x86_64.so

install: libgetdiskspace_x86.so libgetdiskspace_x86_64.so
	cp libgetdiskspace_x86.so ../../Assets/Plugins/PatchKit/x86/libgetdiskspace.so
	cp libgetdiskspace_x86_64.so ../../Assets/Plugins/PatchKit/x86_64/libgetdiskspace.so

clean: libgetdiskspace_x86.so libgetdiskspace_x86_64.so
	rm libgetdiskspace_x86.so
	rm libgetdiskspace_x86_64.so

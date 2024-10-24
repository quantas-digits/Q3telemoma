# Telemoma for Qeust3

This repository will maintain the teleoperation and Unity application code for Quest 3, based on the Telemoma project.
https://github.com/UT-Austin-RobIn/telemoma

New features will be added to `telemoma/components` by implementing the Component abstract methods, `stream()` and `destroy()`, inspired by the Open-Teach project.https://github.com/aadhithya14/Open-Teach

We use `multiprocessing` to run the server, each socket inside the server and teleop seperately to ensure the quality of data transportation. Therefore, a share data structure across processes is required to ensure the communication when lunching the program. Check the demo for details.

# Environment
Setup the same environment as telemoma did.

# VR setup
Currently, we provide a `.apk` file for Oculus quest3, please download here:
https://drive.google.com/file/d/1INr6V8vifQ3L0maXOWlZo0eA1zjt8weY/view?usp=drive_link

# Demo
Lunch the igibson demo by:
```
cd telemoma/components
python demo_igibson.py --robot tiago --teleop_config configs/only_vr.py
```

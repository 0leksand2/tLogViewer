# tLogViewer

The goal is to build a <b>usable</b> log viewer for ArduPilot logs.
Only fixed-wing aircraft are fully supported at the moment. Multi-copters and Rovers are to be implemented.
### Flight Analytics Software for ArduPilot Logs
<img width="2558" height="1352" alt="image" src="https://github.com/user-attachments/assets/88a231fe-8576-4d7c-b2d6-1474ff9b7e1c" />


# How to use
### Upload the tLog file to the server; the server will analyze it and return the flight log in a readable format
<img width="767" height="410" alt="image" src="https://github.com/user-attachments/assets/aa064eaa-6e5b-4fac-ab86-5c510a50304e" />


### Select the needed telemetry properties on the side menu for display
<img width="583" height="1332" alt="image" src="https://github.com/user-attachments/assets/16df7e83-53d6-400b-89e2-dfd256140fd8" />


### Settings in the top right corner allow you to change display settings like heading/gps/trail length/navigation source
<img width="845" height="814" alt="image" src="https://github.com/user-attachments/assets/6ce3738b-fe4f-45ee-9044-f9921db42735" />


### Legend can be found in the top right corner under the Info button
<img width="859" height="1084" alt="image" src="https://github.com/user-attachments/assets/f0554a87-a0e7-46d8-81e2-0252ef080997" />


### Flight analytics can be found in the bottom right corner of the screen to find anomalies during the flight
<img width="1474" height="1252" alt="image" src="https://github.com/user-attachments/assets/24f17cfb-702c-48cc-93b1-aeff1ad24d01" />


### Play timeline contains events that happened during flight time: arm/disarm/change of flight mode
<img width="1956" height="143" alt="image" src="https://github.com/user-attachments/assets/0f8755d6-ed39-4fd3-9b52-d0d0b4ab6360" />


# Build
The application is built using .NET10 and Angular 22. The build is standard.\
Build the ClientApp folder using ```ng build command```, then build and publish ```TLogViewer.Web project```. \
```docker build``` is not guaranteed at this point.







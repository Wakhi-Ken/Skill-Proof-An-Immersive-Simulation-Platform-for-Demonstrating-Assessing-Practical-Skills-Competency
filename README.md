# Skill-Proof

> An immersive VR platform for demonstrating and assessing practical skills and providing performance evidence.

## Description

**Skill-Proof** is a VR capstone project that allows users to perform selected practical skills in an immersive virtual environment and have their performance assessed.

**Problem:** People may have practical skills gained through education, vocational training, informal learning, apprenticeship, or work experience but may have limited practical evidence to demonstrate what they can do. This can make it difficult for employers to understand a person's demonstrated practical ability.

**Solution:** Skill-Proof provides predefined practical VR activities where users perform tasks while the system tracks their actions, assesses their performance, and generates skill evidence that can help communicate demonstrated competency to employers and other stakeholders.

### Key features

* Predefined practical VR activities for electrical wiring and solar panel installation.
* Interactive 3D environments with virtual tools and equipment.
* Performance tracking and assessment.
* Real-time or activity-based feedback and scoring.
* Tracking of accuracy, errors, safety compliance, and completion time.
* Performance results and digital skill evidence.
* User accounts and authentication using Firebase.

## Links

| Resource          | Link                                                                                                                                                    |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| GitHub repository | [Skill-Proof GitHub](https://github.com/Wakhi-Ken/Skill-Proof-An-Immersive-Simulation-Platform-for-Demonstrating-Assessing-Practical-Skills-Competency) |
| Figma designs     | [Skill-Proof on Figma](https://www.figma.com/design/C4MHMWXMz2jBHC4t71BWcv/Skill-Proof?node-id=0-1&p=f&t=zkOPKaTbnOLc6Ktw-0)                            |
| Demo video        | [Skill-Proof Demo](https://youtu.be/gIodQVoGv6o)                                                                                                                                |

## Setup: Environment and Project

### Prerequisites

| Requirement          | Version / Notes          |
| -------------------- | ------------------------ |
| Game engine          | Unity 6                  |
| Programming language | C#                       |
| XR SDK               | XR Interaction Toolkit   |
| VR headset           | Meta Quest 2             |
| Input                | Quest Touch controllers  |
| OS                   | Windows                  |
| Version control      | Git / GitHub             |
| Backend              | Firebase                 |
| Development tools    | Unity Hub, Visual Studio |

### Installation

```bash
# 1. Clone the repository

git clone https://github.com/Wakhi-Ken/Skill-Proof-An-Immersive-Simulation-Platform-for-Demonstrating-Assessing-Practical-Skills-Competency.git

# 2. Enter the project folder

cd Skill-Proof-An-Immersive-Simulation-Platform-for-Demonstrating-Assessing-Practical-Skills-Competency
```

Open the project using **Unity Hub** and select the Unity 6 version used to develop the project.

### Configure the project

1. Open the project in Unity.
2. Open **Edit → Project Settings → XR Plug-in Management**.
3. Configure the Android/VR platform for Meta Quest 2.
4. Open **Window → Package Manager** and ensure the required XR packages are installed.
5. Configure Firebase Authentication and other Firebase services used by the project.
6. Open the **Start Menu** scene.

### Run the project

**In the Unity Editor:**

Press **Play** to test the application.

**On Meta Quest 2:**

1. Enable Developer Mode on the headset.
2. Connect the headset to the development computer.
3. Select **File → Build Profiles** and configure an Android build.
4. Build and install the application on the Meta Quest 2.

##  Designs

### Figma mockups and prototype

Full prototype:

[Open Skill-Proof in Figma](https://www.figma.com/design/C4MHMWXMz2jBHC4t71BWcv/Skill-Proof?node-id=0-1&p=f&t=zkOPKaTbnOLc6Ktw-0)

The Figma prototype covers the main user interface, application flow, simulation selection, practical activities, performance results, and skill evidence.

### Interaction Flow

A[Launch Application] --> B[Sign Up / Login]
B --> C[Simulator Menu]
C --> D[Select Simulation]
D --> E[Select Activity]
E --> F[View Instructions]
F --> G[Perform Practical Task]
G --> H[Track User Actions]
H --> I[Assess Performance]
I --> J[Generate Feedback]
J --> K[Generate Skill Evidence]
K --> L[View Results]
L --> C
```

### System / Hardware Diagram

<img width="864" height="620" alt="image" src="https://github.com/user-attachments/assets/05194017-6b19-41cb-943c-efb4eb283556" />


### App Screenshots

| Screenshot

<img width="1142" height="647" alt="Screenshot 2026-10-02 225710" src="https://github.com/user-attachments/assets/f5c996a0-7b09-4c79-9913-61d2ae5a99ef" />
<img width="1113" height="655" alt="Screenshot 2026-10-02 225655" src="https://github.com/user-attachments/assets/f51de153-877e-4d40-bcf9-2b194fb42b3f" />
<img width="1107" height="610" alt="Screenshot 2026-10-02 225534" src="https://github.com/user-attachments/assets/0d1ad96d-cd78-4185-a798-f0157ccf8e74" />
<img width="1705" height="795" alt="Screenshot 2026-10-02 225453" src="https://github.com/user-attachments/assets/5aaca9db-6910-42a5-95d4-5bc567801eb9" />
<img width="1027" height="635" alt="Screenshot 2026-10-03 001122" src="https://github.com/user-attachments/assets/1eae4198-c355-4410-ad30-1f8df5db5292" />
<img width="1003" height="488" alt="Screenshot 2026-10-03 000939" src="https://github.com/user-attachments/assets/9a04aab3-e1b4-4551-8a2e-2f598c7957d0" />
<img width="1128" height="645" alt="Screenshot 2026-10-02 230042" src="https://github.com/user-attachments/assets/290010fc-8991-4b60-b40e-47adf039cd9e" />



##  Assets

| Asset                       | Type      | Purpose / Justification                              | Source            |
| --------------------------- | --------- | ---------------------------------------------------- | ----------------- |
| Virtual tools and equipment | 3D models | Used to create the practical simulation environments | Own / Free assets |
| Electrical components       | 3D models | Used for the electrical wiring simulation            | Own / Free assets |
| Solar panel components      | 3D models | Used for the solar installation simulation           | Own / Free assets |
| UI icons                    | 2D        | Used for menus and interface elements                | Own / Free assets |


##  Hardware Integration

* **Device:** Meta Quest 2
* **Input:** Quest Touch controllers
* **Development platform:** Unity 6 + XR Interaction Toolkit
* **Programming:** C#

| User action        | Hardware input          | Result in the experience           |
| ------------------ | ------------------------ | ----------------------------------- |
| Select an object   | Controller trigger      | Object is selected/interacted with |
| Grab an object     | Controller trigger/grip | Virtual object can be manipulated  |
| Move around        | Controller thumbstick   | User moves through the environment |
| Look around        | Head movement            | User views the virtual environment |
| Complete an action | Controller interaction  | System records the action          |
| Pause              | UI button                | Simulation is paused               |

##  Deployment Plan

1. **Build:** Create an Android build of the Unity application for Meta Quest 2.
2. **Testing:** Install the application on the headset and test the two practical simulations.
3. **User Evaluation:** Test the prototype with a selected sample of users.
4. **Backend:** Use Firebase Authentication, Cloud Firestore, and Cloud Storage for the required online services.
5. **Distribution:** Initially distribute the application through direct APK installation/sideloading for testing.
6. **Future Deployment:** A future version could be distributed through an appropriate Meta Quest application distribution platform.
7. **Maintenance:** Continue fixing bugs and improving the application based on testing and user feedback.

##  Project Structure

```text
Skill-Proof/
│
├── Assets/
│   ├── Scenes/
│   ├── Scripts/
│   ├── Prefabs/
│   ├── Models/
│   └── Materials/
│
├── docs/
│   ├── designs/
│   └── screenshots/
│
├── builds/
│
└── README.md
```

## Project Scope

The current prototype contains two predefined practical simulations workflow for users:

1. **Electrical Wiring Installation**
2. **Solar Panel Installation**

The project does not currently include:

* full intergrated system logic to test competency and more
* record video feature
* working genaration evidence system
* Official professional certification

##  Developer

| Name            | Role                                     |
| --------------- | ----------------------------------------- |
| Wakhile Dlamini | Software Engineering Student / Developer |



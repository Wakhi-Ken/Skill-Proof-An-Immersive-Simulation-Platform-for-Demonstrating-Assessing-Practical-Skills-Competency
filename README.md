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
| Demo video        | `<[Add demo video link](https://youtu.be/gIodQVoGv6o)>`                                                                                                                                 |

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

## 🎨 Designs

### Figma mockups and prototype

Full prototype:

[Open Skill-Proof in Figma](https://www.figma.com/design/C4MHMWXMz2jBHC4t71BWcv/Skill-Proof?node-id=0-1&p=f&t=zkOPKaTbnOLc6Ktw-0)

The Figma prototype covers the main user interface, application flow, simulation selection, practical activities, performance results, and skill evidence.

| Screen                | Preview                                          |
| --------------------- | ------------------------------------------------ |
| Start Menu            | `![Start Menu](docs/designs/start-menu.png)`     |
| Login                 | `![Login](docs/designs/login.png)`               |
| Simulator Menu        | `![Simulator](docs/designs/simulator.png)`       |
| Activity Instructions | `![Instructions](docs/designs/instructions.png)` |
| Results               | `![Results](docs/designs/results.png)`           |
| Skill Evidence        | `![Evidence](docs/designs/evidence.png)`         |

### Interaction Flow

```mermaid
flowchart TD

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

Because Skill-Proof is a VR software application, a **system/hardware block diagram** is used instead of a traditional electrical circuit diagram.

```mermaid
flowchart LR

U[User] -->|Head movement| HMD[Meta Quest 2]
U -->|Controller input| CTRL[Quest Controllers]

HMD -->|Head tracking| APP[Skill-Proof VR App<br/>Unity + C#]
CTRL -->|Interaction input| APP

APP -->|3D visuals + audio| HMD
APP -->|Haptic feedback| CTRL

APP --> AS[Assessment System]

AS --> TRACK[Performance Tracking]
AS --> SCORE[Scoring]
AS --> FEEDBACK[Feedback]
AS --> EVIDENCE[Skill Evidence]

APP <-->|Authentication and data| FB[Firebase]

FB --> AUTH[Firebase Authentication]
FB --> FS[Cloud Firestore]
FB --> STORAGE[Cloud Storage]
```

### App Screenshots

| Screenshot                                         | Description                   |
| --------------------------------------------------- | ------------------------------ |
| `![Screenshot 1](<img width="1128" height="645" alt="Screenshot 2026-10-02 230042" src="https://github.com/user-attachments/assets/83cf3296-5391-4bab-b8d2-ba3864fffad5" />
)`      | Login interface               |
| `![Screenshot 2](<img width="1107" height="610" alt="Screenshot 2026-10-02 225534" src="https://github.com/user-attachments/assets/d08d1539-ba58-4d0e-8fe4-08962457ede2" />
)`  | Simulator selection interface |
| `![Screenshot 3](<img width="1142" height="647" alt="Screenshot 2026-10-02 225710" src="https://github.com/user-attachments/assets/0ac0eb80-bf8d-439c-88c8-abe4ca68339e" />
)` | VR practical simulation       |
| `![Screenshot 4](<img width="1027" height="635" alt="Screenshot 2026-10-03 001122" src="https://github.com/user-attachments/assets/2a2165da-adcd-4b0f-9957-95d095aea4fb" />

)`    | Performance results           |
| `![Screenshot 5](<img width="1003" height="488" alt="Screenshot 2026-10-03 000939" src="https://github.com/user-attachments/assets/27bd2968-5eda-489f-972b-4884fcb71dce" />
)`   | Skill evidence                |

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



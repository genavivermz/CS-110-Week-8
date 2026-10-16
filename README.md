# Project Brief | Operations Hub
- Functions as an all-in-one work hub for learners navigating between lecture links, notepads, and timing.
- Utilizes a pink theme

### Assumptions:
- Assumed user would feel comfortable with timer controls (start, pause, reset) + would be able to identify and differentiate each control.
### Outcome:
- User could ID controls, but naturally wanted to reuse start button for pause control.
### Next fix:
- Implement a two-in-one start/pause button for timer.

# Directory
#### Evidence/8A/
- Includes 8A prototype, whiteboard flow, and 8A Discord peer check observations.

#### Evidence/8B/
- Includes 8B Discord peer check review observation, whiteboard, and receipt.

# Validation & Recovery
- Rejects invalid input, recovery message, export & reopen text record functionality.

# AI Check
- Response: "The timer currently resets to 25:00 when the WPF app is reopened: its remaining time only lives in memory, and closing the app ends the timer. The smallest useful change is to save the remaining time when closing and restore it when the app starts, so closing pauses the timer and reopening lets the user continue. Put that change in MainWindow.xaml.cs, around the constructor/LoadWorkspace, SaveWorkspace, and Window_Closing methods. Test it by starting the timer, waiting two minutes, closing and reopening the app, and checking that it shows about 23:00; click Start and confirm it continues."
- **Kept** the simple functionality of closing browser clearing the timer; avoiding an unnecessary memory step since we will cover that soon.

# Operations Hub: raw learning template

Open OperationsHub.csproj in Visual Studio. The interface exists; C# handlers contain TODO comments. Follow operations-quick-start.html from beginning to end.

Read the method-placement notes before copying. Replace a named handler; never duplicate it or paste a method inside another method.

The completed teaching code lives in the illustrated guide, not in this ZIP.

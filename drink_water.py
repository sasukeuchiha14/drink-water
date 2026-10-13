from plyer import notification
from pygame import mixer
import time
import threading
import os

# Get the directory of the current script
current_dir = os.path.dirname(os.path.abspath(__file__))

# Play sound using pygame mixer for 5 seconds
def play_sound():
    mixer.init()
    mixer.music.load(os.path.join(current_dir, "water.mp3"))
    mixer.music.play()
    time.sleep(5)
    mixer.music.stop()

# Show notification using plyer for 5 seconds with a fact
def show_notification():
    notification.notify(
        title = "Drink Water Reminder",
        message = "Fact: Men should have about 2.6 litres (10 cups) of fluids a day.",
        app_icon = os.path.join(current_dir, "logo.ico"),
        timeout = 5
    )

# Main function
if __name__ == "__main__":
    while True:
        # Create two threads for playing sound and showing notification
        sound_thread = threading.Thread(target=play_sound)
        notification_thread = threading.Thread(target=show_notification)

        # Start the threads
        sound_thread.start()
        notification_thread.start()

        # Wait for the threads to finish
        sound_thread.join()
        notification_thread.join()

        # Wait for 30 minutes before showing the next reminder
        time.sleep(30*60)

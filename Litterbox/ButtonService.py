import RPi.GPIO as GPIO
import time
import datetime;
from systemd import journal

timer = 1800

spin_output = 2
cancel_output = 3

spin_button = 20
cancel_button = 21

GPIO.setmode(GPIO.BCM)

GPIO.setup(spin_output, GPIO.OUT)
GPIO.setup(cancel_output, GPIO.OUT)
GPIO.setup(spin_button, GPIO.IN, pull_up_down=GPIO.PUD_UP)
GPIO.setup(cancel_button, GPIO.IN, pull_up_down=GPIO.PUD_UP)

try:
    GPIO.output(spin_output, GPIO.LOW)
    GPIO.output(cancel_output, GPIO.LOW)

    while True:
        if GPIO.input(spin_button) == GPIO.LOW:
            journal.write("Spin button pressed. Outputting signal.")
            GPIO.output(spin_output, GPIO.HIGH)
            time.sleep(0.75)
            GPIO.output(spin_output, GPIO.LOW)
        elif GPIO.input(cancel_button) == GPIO.LOW:
            journal.write("Cancel button pressed. Outputting signal.")
            GPIO.output(cancel_output, GPIO.HIGH)
            time.sleep(0.75)
            GPIO.output(cancel_output, GPIO.LOW)
        else:
            if timer == 0:
                journal.write("Cancel time reached. Outputting signal, resetting timer.")
                GPIO.output(cancel_output, GPIO.HIGH)
                time.sleep(0.75)
                GPIO.output(cancel_output, GPIO.LOW)
                timer = 1800
            else:
                timer = timer - 1
                time.sleep(1)
finally:
    GPIO.cleanup()

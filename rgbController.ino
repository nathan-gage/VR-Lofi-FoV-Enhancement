#include <FastLED.h>

#define NUM_LEDS    20
#define DATA_PIN    2
#define BRIGHTNESS  35

CRGB leds[NUM_LEDS];
CRGB oldLeds[NUM_LEDS];
CRGB targetLeds[NUM_LEDS];

float interval = 0.0;
uint8_t numColors = 0;

void setup() {
  Serial.begin(115200);
  FastLED.addLeds<WS2812B, DATA_PIN, GRB>(leds, NUM_LEDS);
  FastLED.setBrightness(BRIGHTNESS);
}

void loop() {
  if (Serial.available()) {
    String input = Serial.readStringUntil('>');
    int startPos = input.lastIndexOf('<');
    if (startPos != -1) {
      input = input.substring(startPos + 1);

      int separatorPos = input.indexOf('|');
      interval = input.substring(0, separatorPos).toFloat();

      input = input.substring(separatorPos + 1);
      separatorPos = input.indexOf('|');
      numColors = input.substring(0, separatorPos).toInt();

      if (numColors <= NUM_LEDS) {
        for (int i = 0; i < NUM_LEDS; i++) {
          oldLeds[i] = leds[i];
        }

        for (int i = 0; i < numColors; i++) {
          input = input.substring(separatorPos + 1);
          separatorPos = input.indexOf('|');
          String colorString = input.substring(0, separatorPos);
          int r = colorString.substring(0, colorString.indexOf(',')).toInt();
          colorString = colorString.substring(colorString.indexOf(',') + 1);
          int g = colorString.substring(0, colorString.indexOf(',')).toInt();
          int b = colorString.substring(colorString.indexOf(',') + 1).toInt();
          targetLeds[i] = CRGB(r, g, b);
        }

        unsigned long startTime = millis();
        while (millis() - startTime < interval * 1000) {
          float t = (float)(millis() - startTime) / (interval * 1000);
          for (int i = 0; i < NUM_LEDS; i++) {
            leds[i].r = oldLeds[i].r + (targetLeds[i].r - oldLeds[i].r) * t;
            leds[i].g = oldLeds[i].g + (targetLeds[i].g - oldLeds[i].g) * t;
            leds[i].b = oldLeds[i].b + (targetLeds[i].b - oldLeds[i].b) * t;
          }
          FastLED.show();
        }
      }
    }
  }
}


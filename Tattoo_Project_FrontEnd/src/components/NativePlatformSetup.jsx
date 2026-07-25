import { useEffect } from "react";
import { Capacitor } from "@capacitor/core";
import { Keyboard, KeyboardResize } from "@capacitor/keyboard";
import { StatusBar, Style } from "@capacitor/status-bar";

function NativePlatformSetup() {
  useEffect(() => {
    if (!Capacitor.isNativePlatform()) {
      return;
    }

    const configureNativeShell = async () => {
      const tasks = [
        StatusBar.setStyle({ style: Style.Light }),
        StatusBar.setOverlaysWebView({ overlay: false }),
        Keyboard.setResizeMode({ mode: KeyboardResize.Body }),
      ];

      if (Capacitor.getPlatform() === "android") {
        tasks.push(StatusBar.setBackgroundColor({ color: "#09090d" }));
      }

      await Promise.allSettled(tasks);
    };

    configureNativeShell();
  }, []);

  return null;
}

export default NativePlatformSetup;

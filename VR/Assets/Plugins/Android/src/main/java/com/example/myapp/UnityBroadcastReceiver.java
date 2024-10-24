package com.example.myapp;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import com.unity3d.player.UnityPlayer;

public class UnityBroadcastReceiver extends BroadcastReceiver {
    @Override
    public void onReceive(Context context, Intent intent) {
        String action = intent.getAction();
        if ("com.example.TRIGGER_OVR_INPUT".equals(action)) {
            UnityPlayer.UnitySendMessage("AdbMessageSender", "readControllerState", "");
        }
    }
}

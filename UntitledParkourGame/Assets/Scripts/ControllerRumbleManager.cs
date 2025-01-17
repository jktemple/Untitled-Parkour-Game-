using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerRumbleManager : MonoBehaviour
{
    [SerializeField]
    PlayerInput _playerInput;
    [SerializeField]
    bool controllerTest;
    private RumblePattern activeRumblePattern;
    private float rumbleDurration;
    private float pulseDurration;
    //private float burstDurration;
    private float lowA;
    private float lowStep;
    private float highA;
    private float highStep;
    private float rumbleStep;
    private bool isMotorActive = false;


    public enum RumblePattern
    {
        Constant,
        Pulse,
        Linear,
        Burst
    }
    
    // Public Methods
    public void RumbleConstant(float low, float high, float durration)
    {
        activeRumblePattern = RumblePattern.Constant;
        lowA = low;
        highA = high;
        rumbleDurration = Time.time + durration;
    }

    public void RumblePulse(float low, float high, float burstTime, float durration)
    {
        activeRumblePattern = RumblePattern.Pulse;
        lowA = low;
        highA = high;
        rumbleStep = burstTime;
        pulseDurration = Time.time + burstTime;
        rumbleDurration = Time.time + durration;
        isMotorActive = true;
        var g = GetGamepad();
        g?.SetMotorSpeeds(lowA, highA);
    }

    public void RumbleLinear(float lowStart, float lowEnd, float highStart, float highEnd, float durration)
    {
        activeRumblePattern = RumblePattern.Linear;
        lowA = lowStart;
        highA = highStart;
        lowStep = (lowEnd - lowStart) / durration;
        highStep = (highEnd - highStart) / durration;
        rumbleDurration = Time.time + durration;
    }

    public void RumbleBurst(float low, float high, float burstTime)
    {
        activeRumblePattern = RumblePattern.Burst;
        lowA = low;
        highA = high;
        rumbleDurration = Time.time + burstTime;
    }

    public void StopRumble()
    {
        //Debug.Log("Stopping Rumble");
        var gamepad = GetGamepad();
        //Debug.Log(gamepad.ToString());
        gamepad?.SetMotorSpeeds(0, 0);
        rumbleDurration = 0;
    }



    private void Update()
    {
        var gamepad = GetGamepad();
        if (gamepad == null)
        {
            return;
        }
        else
        {
            //Debug.Log("current gamepad =" + gamepad?.name + " Display name = " + gamepad?.deviceId);
        }

        if (Time.time > rumbleDurration)
        {
            StopRumble();
            return;
        }
        
        
        if(_playerInput.currentControlScheme != "Gamepad" && !controllerTest)
        {
            return;
        }

       


        switch (activeRumblePattern)
        {
            case RumblePattern.Constant:
                gamepad.SetMotorSpeeds(lowA, highA);
                break;

            case RumblePattern.Pulse:

                if (Time.time > pulseDurration)
                {
                    isMotorActive = !isMotorActive;
                    pulseDurration = Time.time + rumbleStep;
                    if (!isMotorActive)
                    {
                        gamepad.SetMotorSpeeds(0, 0);
                    }
                    else
                    {
                        gamepad.SetMotorSpeeds(lowA, highA);
                    }
                }

                break;
            case RumblePattern.Linear:
                gamepad.SetMotorSpeeds(lowA, highA);
                lowA += (lowStep * Time.deltaTime);
                highA += (highStep * Time.deltaTime);
                break;
            case RumblePattern.Burst:
                gamepad.SetMotorSpeeds(lowA,highA); 
                break;
            default:
                break;
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        StopRumble();
    }

    // Private helpers

    private Gamepad GetGamepad()
    {
        return Gamepad.current;
    }
}

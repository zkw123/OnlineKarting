﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{ 
    public bool IsFinite { get; private set; }
    public float TotalTime { get; private set; }
    public float TimeRemaining { get; private set; }
    public static bool IsOver;

    public bool raceStarted { get; set; }

    public static Action<float> OnAdjustTime;
    public static Action<int, bool, GameMode> OnSetTime;

    private void Awake()
    {
        IsFinite = false;
        TimeRemaining = 0f;
    }


    void OnEnable()
    {
        OnAdjustTime += AdjustTime;
        OnSetTime += SetTime;
    }

    private void OnDisable()
    {
        OnAdjustTime -= AdjustTime;
        OnSetTime -= SetTime;
    }

    private void AdjustTime(float delta)
    {
        TimeRemaining += delta;
    }

    private void SetTime(int time, bool isFinite, GameMode gameMode)
    {
        TotalTime = time;
        IsFinite = isFinite;
        TimeRemaining = 0;
    }

    void Update()
    {
        if (!raceStarted)
        {
            TimeRemaining = 0;
            return;
        }
        KartPlayer.GameStart = raceStarted;
        
        if (IsFinite && !IsOver&&raceStarted)
        {
            TimeRemaining += Time.deltaTime;
           
        }
    }

    public void StartRace()
    {
        raceStarted = true;
    }

    public void StopRace() {
        raceStarted = false;
    }
}


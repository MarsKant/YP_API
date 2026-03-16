package com.example.androidupproject.models;

public class AddPointsRequest {
    public int Points;
    public String Action;

    public AddPointsRequest(int points, String action) {
        this.Points = points;
        this.Action = action;
    }
}
package com.example.androidupproject.models;

import com.google.gson.annotations.SerializedName;

public class PointsResponse {
    public boolean success;
    public String message;
    public PointsData data;

    public static class PointsData {
        @SerializedName(value = "points", alternate = {"Points"})
        public int points;

        @SerializedName(value = "level", alternate = {"Level"})
        public int level;

        @SerializedName(value = "nextLevelPoints", alternate = {"NextLevelPoints"})
        public int nextLevelPoints;

        @SerializedName(value = "progress", alternate = {"Progress"})
        public double progress;

        @SerializedName(value = "leveledUp", alternate = {"LeveledUp"})
        public boolean leveledUp;

        public PointsData() {}
    }
}
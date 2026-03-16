package com.example.androidupproject.models;

import com.google.gson.annotations.SerializedName;
import java.util.List;

public class LeaderboardResponse {
    public boolean success;
    public List<LeaderboardItem> data;

    public static class LeaderboardItem {
        @SerializedName(value = "userId", alternate = {"UserId"})
        public int userId;

        @SerializedName(value = "username", alternate = {"Username"})
        public String username;

        @SerializedName(value = "points", alternate = {"Points"})
        public int points;

        @SerializedName(value = "level", alternate = {"Level"})
        public int level;

        public LeaderboardItem() {}
    }
}
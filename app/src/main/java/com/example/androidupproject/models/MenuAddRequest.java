package com.example.androidupproject.models;

public class MenuAddRequest {
    public int userId;
    public int recipeId;
    public String date;

    public MenuAddRequest(int userId, int recipeId, String date) {
        this.userId = userId;
        this.recipeId = recipeId;
        this.date = date;
    }
}
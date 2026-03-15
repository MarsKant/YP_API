package com.example.androidupproject.models;

public class ImageGenerationRequest {
    public String query;
    public int RecipeId;

    public ImageGenerationRequest(String query, int recipeId) {
        this.query = query;
        this.RecipeId = recipeId;
    }
}
package com.example.androidupproject.models;

import com.google.gson.annotations.SerializedName;

public class IngredientDto {
    @SerializedName(value = "Id", alternate = {"id"})
    public int id;

    @SerializedName(value = "Name", alternate = {"name", "ProductName", "productName"})
    public String name;

    @SerializedName(value = "Unit", alternate = {"unit"})
    public String unit;

    @SerializedName(value = "Category", alternate = {"category"})
    public String category;

    @SerializedName(value = "IngredientId", alternate = {"ingredientId"})
    public int ingredientId;

    // Добавляем поле quantity
    @SerializedName(value = "Quantity", alternate = {"quantity"})
    public double quantity;

    public IngredientDto() {}

    public IngredientDto(String name, String unit) {
        this.name = name;
        this.unit = unit;
        this.category = "Разное";
        this.id = 1;
        this.ingredientId = 1;
        this.quantity = 1;  // значение по умолчанию
    }

    public int getDeleteId() {
        if (ingredientId != 0) return ingredientId;
        return id;
    }
}
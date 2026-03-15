package com.example.androidupproject.models;

import com.google.gson.annotations.SerializedName;
import java.util.List;

public class MenuDto {
    @SerializedName("id")
    public int id;

    @SerializedName("name")
    public String name;

    @SerializedName("items")
    public List<MenuItemDto> items;

    @SerializedName("createdAt")
    public String createdAt;
}
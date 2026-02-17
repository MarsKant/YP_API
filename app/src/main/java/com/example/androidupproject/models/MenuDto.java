package com.example.androidupproject.models;
import com.google.gson.annotations.SerializedName;
import java.util.List;

public class MenuDto {
    @SerializedName(value = "Id", alternate = {"id"})
    public int id;

    @SerializedName(value = "Name", alternate = {"name"})
    public String name;

    @SerializedName(value = "Items", alternate = {"items", "menuItems", "MenuItems"})
    public List<MenuItemDto> items;
}
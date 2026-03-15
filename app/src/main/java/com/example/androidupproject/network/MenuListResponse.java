package com.example.androidupproject.network;

import com.example.androidupproject.models.MenuDto;
import java.util.List;

public class MenuListResponse {
    public boolean success;
    public String message;
    public List<MenuDto> data;
}
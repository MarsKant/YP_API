package com.example.androidupproject.models;

import com.google.gson.annotations.SerializedName;

public class LoginResponse {
    @SerializedName("success")
    public boolean success;

    @SerializedName("id")
    public int id;

    @SerializedName("username")
    public String username;

    @SerializedName("email")
    public String email;

    @SerializedName("message")
    public String message;

    @SerializedName("isAdmin")
    public boolean isAdmin;

    @SerializedName("token")
    public String token;
}
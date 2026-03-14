package com.example.androidupproject.models;

import com.google.gson.annotations.SerializedName;

public class RegisterRequest {
    @SerializedName("login")
    public String login;

    @SerializedName("email")
    public String email;

    @SerializedName("password")
    public String password;

    // --- ДОБАВЬТЕ ЭТОТ БЛОК ---
    public RegisterRequest(String login, String email, String password) {
        this.login = login;
        this.email = email;
        this.password = password;
    }
    // --------------------------

    // Также рекомендуется оставить пустой конструктор для библиотеки GSON
    public RegisterRequest() {}
}
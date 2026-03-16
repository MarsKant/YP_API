package com.example.androidupproject;

import android.content.Context;
import android.content.Intent;

import com.example.androidupproject.models.GameActivity;
import com.google.android.material.bottomnavigation.BottomNavigationView;
import com.example.androidupproject.models.MenuActivity;
import com.example.androidupproject.models.ShoppingListActivity;
import com.example.androidupproject.models.FavoritesActivity;

public class NavigationHelper {

    public static void setupNavigation(Context context, BottomNavigationView view, int selectedItemId) {
        view.setSelectedItemId(selectedItemId);

        view.setOnItemSelectedListener(item -> {
            int itemId = item.getItemId();

            if (itemId == selectedItemId) {
                return true;
            }

            Intent intent = null;

            if (itemId == R.id.nav_fridge) {
                intent = new Intent(context, MainActivity.class);
            } else if (itemId == R.id.nav_menu) {
                intent = new Intent(context, MenuActivity.class);
            } else if (itemId == R.id.nav_shop) {
                intent = new Intent(context, ShoppingListActivity.class);
            } else if (itemId == R.id.nav_fav) {
                intent = new Intent(context, FavoritesActivity.class);
            } else if (itemId == R.id.nav_game) {
                intent = new Intent(context, GameActivity.class);
            }


            if (intent != null) {
                // Убираем анимацию для плавного перехода
                intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
                context.startActivity(intent);
                return true;
            }
            return false;
        });
    }
}
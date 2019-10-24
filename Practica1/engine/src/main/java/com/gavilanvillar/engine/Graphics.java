package com.gavilanvillar.engine;

public interface Graphics {

    public Image newImage(String name);

    void clear(int color);

    /**
     * Dibuja completamente la imagen "image" en la posición (x, y) de la pantalla/ventana.
     * Mapea cada pixel de la imagen en mi pixel de pantalla.
     *
     * @param image Imagen a pintar
     * @param x Posición x en la pantalla/ventana
     * @param y Posición y en la pantalla/ventana
     */
    void drawImage(Image image, int x, int y);

    int getWidht();

    int getHeight();

}

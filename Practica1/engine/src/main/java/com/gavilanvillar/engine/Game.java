package com.gavilanvillar.engine;

/**
 * Interfaz Game
 */
public interface Game {
    /**
     * Devuelve la instancia del "motor" gráfico
     * @return  Graphics
     */
    Graphics getGraphics();

    /**
     * Devuelve la entrada del gestor de salida
     * @return  Input
     */
    Input getInput();

    //void run();
}

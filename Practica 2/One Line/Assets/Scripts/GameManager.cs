﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Tooltip("Archivos .json que almacenan los niveles.")]
    public List<TextAsset> _categoryLevelFiles;

    public LevelsGroup _levelsGroup;

    //---------------------------------------------------
    //TEMPORAL
    [Tooltip("Nombre de la categoría del nivel al que quieres acceder.")]
    [Range(1, 5)]
    public int _categoryLevel = 0;

    [Tooltip("Nivel del juego.")]
    public int _level = 0;

    private bool _screenSizeIsChanged = false;
    //---------------------------------------------------



    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;

        _levelsGroup = new LevelsGroup();
        _levelsGroup.LoadLevelsFromJSON(_categoryLevelFiles);


    }

    private void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetScreenSizeIsChanged(bool b)
    {
        _screenSizeIsChanged = b;    
    }

    public bool IsScreenSizeChanged()
    {
        return _screenSizeIsChanged;
    }

    /// <summary>
    /// Devuelve el nivel los datos del nivel elegido por el jugador
    /// </summary>
    /// <returns>Datos del nivel</returns>
    public LevelData GetLevel()
    {
        return _levelsGroup._levels[_categoryLevel - 1][_level - 1];
    }

    public List<int> GetAvailableSpace()
    {
        return _levelsGroup._levelsAvailableSpace[_categoryLevel - 1];
    }

    public List<int> GetSize()
    {
        return _levelsGroup._levelsSize[_categoryLevel - 1];
    }
}

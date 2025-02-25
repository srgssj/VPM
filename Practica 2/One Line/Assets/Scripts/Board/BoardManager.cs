﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Clase que controla toda la gestión del tablero: Escalado, posicionamiento,
/// fin del nivel, aparición de hints, vuelta atrás, animación de aparición y animación de desaparición.
/// </summary>
public class BoardManager : MonoBehaviour
{
    #region ATRIBUTOS PUBLICOS

    [Tooltip("Gestor de Recursos que almacena los sprites que formaran el tablero.")]
    public ResourceManager _resourceManager;

    [Tooltip("Script que controla el escalado del tablero")]
    public CanvasSize _canvasSize;

    [Tooltip("Prefab del Tile")]
    public GameObject _tileGameObject;

    [Tooltip("Prefab del Hint")]
    public GameObject _hintGameObject;

    [Tooltip("Prefab del Curosr")]
    public GameObject _cursorGameObject;

    [Tooltip("Numero de casillas que se le quiere mostrar al jugador cuando pide una pista.")]
    public int _tilesShowingWhenUserWantHint;

    [Tooltip("Canvas que se muestra al completar el nivel")]
    public GameObject _nextLevelCanvas;

    [Tooltip("Canvas que se muestra al completar un reto")]
    public GameObject _completedChallengeCanvas;

    [Tooltip("Transform donde se agruparan todos los tiles")]
    public Transform _tilesGroup;

    [Tooltip("Transform donde se agruparan todos los hints")]
    public Transform _hintsGroup;

    [HideInInspector]
    public bool doingScalingDown = false;       //Controla el escalado de 1 a 0 de los tiles al terminar un nivel

    [HideInInspector]
    public bool doingScalingUp = false;         //Controla el escalado de 0 a 1 de los tiles al empezar un nivel
   
    [HideInInspector]
    public bool _levelFinished = false;         //Indica si se ha completado el nivel o no

    #endregion

    #region ATRIBUTOS PRIVADOS

    // Estructura usada para almacenar las coordenadas de las entidades 
    // dentro de las matrices
    struct MatrixPos
    {
        public int _x;
        public int _y;

        public MatrixPos(int x, int y)
        {
            _x = x;
            _y = y;
        }
    }

    // INFORMACION NECESARIA PARA LA CREACION DEL NIVEL

    private LevelData _levelData;   // Informacion del tablero

    private List<int> _levelAvailableSpace; // Espacio total del que dispone el tablero

    private List<int> _levelSize;   // Dimensiones del tablero


    // MATRICES Y ARRAYS QUE ALMACENAN INFORMACION Y ENTIDADES DEL TABLERO

    private bool[,] _pressedTilesMatrix;    // Matriz de booleanos que indica que posicion
                                            // esta pulsada o no. Aquellas posiciones de la
                                            // matriz que no tenga una entidad correspondiente,
                                            // es decir, que haya un espacio vacio en el tablero,
                                            // estaran a "true" por defecto para que en las
                                            // posteriores comprobaciones del tablero finalizado
                                            // no supongan ningun problema.

    private Tile[,] _tilesMatrix;   // Matriz de entidades "Tile". En las posiciones vacias tendra
                                    // valor "null".

    private Hint[,] _hintVerticalMatrix;    // Matriz formada por "Hint" en vertical. 
                                            // Tamaño BoardWidth x BoardHeight - 1.

    private Hint[,] _hintHorizontalMatrix;  // Matriz formada por "Hint" en horizontal. 
                                            // Tamaño BoardWidth - 1 x BoardHeight.

    private Hint[] _pathArray;  // Array de "Hint" que contiene las entidades que marcan el camino que
                                // vera el jugador si pide una pista.

    private Stack<MatrixPos> _pathStack;    // Pila de posiciones que almacena el camino que esta recorriendo
                                            // el jugador.

    private Vector2 _emptySpaceSize; // Almacena el tamaño del espacio del que dispone el tablero.

    private Vector2 _emptyCenterPosition; // Almacena la posicion del espacio central vacio.

    private Vector2 _tileSize;  // Guarda el tamaño de un tile


    private int _boardHeight = 0;   // Altura del tablero.

    private int _boardWidth = 0;    // Anchura del tablero.

    private int _tilesShowing;  // Contador que almacena el numero de posiciones totales que se tiene que
                                // mostrar al pedir una pista.

    private int _hintCont = 0;  // Contador que sabe el numero de posiciones que esta mostrando.

    private MatrixPos _firsTile; // Almacena la posicion del punto de inicio del tablero.

    private int _actualColor = 0;   // Almacena el color actual del tablero.

    #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                                                                                 ///
            ///                                     MÉTODOS PÚBLICOS                                            ///
            ///                                                                                                 ///
            ///////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Elimina tiles pulsados hasta llegar al inicial
    /// </summary>
    public void GoBackToInitial()
    {
        GoBack(_firsTile._x, _firsTile._y);
    }

    /// <summary>
    /// Método que resetea el tablero al estado inicial
    /// </summary>
    public void Replay()
    {
        GoBack(_firsTile._x, _firsTile._y);
    }

    /// <summary>
    /// Crea el camino cuando el jugador pide una ayuda. La longitud del camino esta determinada por la
    /// variable "_tilesShowingWhenUserWantHint". Cada vez que el jugador requiere ayuda, se sigue
    /// el camino por donde estaba y vuelve a mostrar la misma cantidad de tiles.
    /// </summary>
    public void UserWantHint()
    {
        if (_hintCont < _levelData._path.Count - 1)
        {
            if (_hintCont % _tilesShowingWhenUserWantHint == 0)
                Replay();

            _pathArray[_hintCont].gameObject.SetActive(true);

            _hintCont++;

            if (_hintCont < _tilesShowing)
            {
                Invoke("UserWantHint", 0.15f);
            }
            else
            {
                _tilesShowing += _tilesShowingWhenUserWantHint;
            }
        }
    }

    /// <summary>
    /// Controla el paso al siguiente nivel, activando el canvas de siguiente nivel, avisando al Game Manager que hay que pasar de nivel
    /// y haciendo la animacion de escalado
    /// </summary>
    public void NextLevel()
    {
        _nextLevelCanvas.SetActive(false);

        GameManager.Instance.NextLevel();
        PrepareLevel();
        StartCoroutine("StartScaleUpAnim");

    }
    
    /// <summary>
    /// Comprueba si se han mostrado todas las ayudas posibles de un tablero
    /// </summary>
    /// <returns>True si se han mostrado todos los hints posibles</returns>
    public bool AllHintsShowed()
    {
        return _hintCont >= _levelData._path.Count - 1;
    }



    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    ///                                                                                                 ///
    ///                                     MÉTODOS PRIVADOS                                            ///
    ///                                                                                                 ///
    ///////////////////////////////////////////////////////////////////////////////////////////////////////


    /// <summary>
    /// Método que llama a todos los metodos necesarios para preparar el nivel, es decir: reiniciar todas las variables,
    /// calcula el tamaño del grid y lo ajusta a este, inicializa las matrices,pilas y arrays al nuevo tablero, limpia 
    /// el tablero anterior y crea el camino solución.
    /// </summary>
    private void PrepareLevel()
    {
        //Reset de las variables
        _levelData = GameManager.Instance.GetLevel();
        _levelAvailableSpace = GameManager.Instance.GetAvailableSpace();
        _levelSize = GameManager.Instance.GetSize();
        _hintCont = 0;
        _tilesShowing = _tilesShowingWhenUserWantHint;

        //Calculo del grid
        SetGridSize();

        //Inicializacion
        InitializeMatrix();

        //Limpieza y creación del grid y creación del path
        ClearGrid();
        CreateGrid();
        CreatePath();
    }

    /// <summary>
    /// Limpia los tiles del tablero ( que se encuentran como hijos de _tilesGroup)
    /// </summary>
    private void ClearGrid()
    {
        foreach (Transform c in _tilesGroup)
        {
            Destroy(c.gameObject);
        }
    }
    /// <summary>
    /// Limpia las ayudas del tablero ( que se encuentran como hijos de _hintsGroup)
    /// </summary>
    private void ClearHints()
    {
        foreach (Transform c in _hintsGroup)
        {
            Destroy(c.gameObject);
        }
    }

    /// <summary>
    /// Co rutina que realiza el escalado ( haciendo que se desvanezcan los tiles ) al acabar un nivel
    /// y avisando al manager de anuncios para que muestre el anuncio entre niveles
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartScaleDownAnim()
    {
        doingScalingDown = true;
        yield return new WaitForSeconds(2.5f);
        doingScalingDown = false;
        _nextLevelCanvas.SetActive(true);
        AdsManager.Instance.ShowTransitionAd();


    }

    /// <summary>
    /// Co rutina que realiza el escalado ( haciendo que se aparezcan los tiles ) al empezar un nivel
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartScaleUpAnim()
    {
        doingScalingUp = true;
        yield return new WaitForSeconds(2.5f);
        doingScalingUp = false;
        _levelFinished = false;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x">Posicion X en coordenadas de matriz</param>
    /// <param name="y">Posicion Y en coordenadas de matriz</param>
    /// <returns>Devuelve true si el Tile en (x, y) fue el ultimo pulsado.</returns>
    private bool IsPeekOfPath(int x, int y)
    {
        return (x >= 0 && x < _boardWidth && y >= 0 && y < _boardHeight)
            && (x == _pathStack.Peek()._x && y == _pathStack.Peek()._y);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x">Posicion X en coordenadas de matriz</param>
    /// <param name="y">Posicion Y en coordenadas de matriz</param>
    /// <returns>Devuelve true si el Tile en (x, y) esta pulsado</returns>
    private bool IsTilePressed(int x, int y)
    {
        return (x >= 0 && x < _boardWidth && y >= 0 && y < _boardHeight)
            && (_pressedTilesMatrix[y, x] && _tilesMatrix[y, x] != null);
    }


    /// <summary>
    /// Muestra el hud de nivel completado para poder pasar al siguiente
    /// </summary>
    private void LevelFinished()
    {
        _levelFinished = true;
        if (GameManager.Instance._doingChallenge)
        {
            _completedChallengeCanvas.SetActive(true);
            GameManager.Instance._doingChallenge = false;
            return;
        }


        if (ProgressManager.Instance.GetUnlockedLevelsOfCategory(GameManager.Instance._categoryLevel) <= GameManager.Instance._level)
        {
            ProgressManager.Instance.LevelCompleted(GameManager.Instance._categoryLevel - 1);
        }

        if (_nextLevelCanvas != null)
        {
            ClearHints();
            StartCoroutine("StartScaleDownAnim");
        }
    }
    /// <summary>
    /// Dada una posicion de ratón calcula la pulsación correspondiente en el tablero
    /// </summary>
    /// <param name="mousePosition">Posicion del raton en la escena</param>
    private void ProcessClickDown(Vector3 mousePosition)
    {
        Vector3 cursorPos = Camera.main.ScreenToWorldPoint(mousePosition);
        cursorPos.z = 0;

        _cursorGameObject.transform.position = cursorPos;

        float x = (mousePosition.x - ((Screen.width - (_boardWidth * _tileSize.x)) / 2)) / _tileSize.x;
        float y = (mousePosition.y - ((Screen.height - (_emptyCenterPosition.y * 2 - Screen.height) - (_boardHeight * _tileSize.y)) / 2)) / _tileSize.y;
        y = -y;

        TilePressed((int)x, (int)y + _boardHeight - 1);
    }

    /// <summary>
    /// Controla la lieración del clic para ver si ha completado el juego o no, si lo ha completado
    /// llama al método que gestiona el fin de nivel.
    /// </summary>
    private void ProcessClickUp()
    {
        _cursorGameObject.SetActive(false);

        bool finished = true;

        for (int y = 0; y < _boardHeight && finished; y++)
        {
            for (int x = 0; x < _boardWidth && finished; x++)
            {
                if (!_pressedTilesMatrix[y, x])
                    finished = false;
            }
        }

        if (finished)
            LevelFinished();

    }

    /// <summary>
    /// Gestiona la pulsacion del Tile.
    /// 
    /// En el caso de que sea un Tile nuevo comprueba si es adyacente del ultimo Tile pulsado. Ademas, marca
    /// el camino que va siguiendo el jugador.
    /// En el caso de que sea un Tile ya pulsado anteriormente elimina todo el camino posteriormente creado. Ademas,
    /// elimina el camino sobrante.
    /// </summary>
    /// <param name="x">Posicion X en coordenadas del mundo</param>
    /// <param name="y">Posicion Y en coordenadas del mundo</param>
    private void TilePressed(int x, int y)
    {

        if ((x >= 0 && x < _boardWidth && y >= 0 && y < _boardHeight) && _tilesMatrix[y, x] != null)
        {

            if (!_pressedTilesMatrix[y, x])
            {
                if (IsPeekOfPath(x - 1, y) || IsPeekOfPath(x + 1, y)
                    || IsPeekOfPath(x, y - 1) || IsPeekOfPath(x, y + 1))
                {
                    _pressedTilesMatrix[y, x] = true;
                    _tilesMatrix[y, x].SetPressed(true);

                    if (_pathStack.Peek()._x != x)
                    {
                        _hintHorizontalMatrix[y, Mathf.Min(_pathStack.Peek()._x, x)].gameObject.SetActive(true);
                    }
                    else if (_pathStack.Peek()._y != y)
                    {
                        _hintVerticalMatrix[Mathf.Min(_pathStack.Peek()._y, y), x].gameObject.SetActive(true);
                    }

                    _pathStack.Push(new MatrixPos(x, y));

                    SoundManager.Instance.PlayMove();
                }
            }
            else if (_pressedTilesMatrix[y, x] && (_pathStack.Peek()._x != x || _pathStack.Peek()._y != y))
            {
                GoBack(x, y);
                SoundManager.Instance.PlayMove();
            }
            _cursorGameObject.SetActive(true);
        }
    }


    /// <summary>
    /// Retrocede el camino ya realizado por el jugador.
    /// </summary>
    /// <param name="x">Posicion X del tablero a la que se quiere retroceder.</param>
    /// <param name="y">Posicion Y del tablero a la que se quiere retroceder.</param>
    private void GoBack(int x, int y)
    {
        while (_pathStack.Count != 0 && (_pathStack.Peek()._x != x || _pathStack.Peek()._y != y))
        {
            MatrixPos pos = _pathStack.Pop();
            _pressedTilesMatrix[pos._y, pos._x] = false;
            _tilesMatrix[pos._y, pos._x].SetPressed(false);

            if (_pathStack.Peek()._x != pos._x)
            {
                _hintHorizontalMatrix[_pathStack.Peek()._y, Mathf.Min(_pathStack.Peek()._x, pos._x)].gameObject.SetActive(false);
            }
            else if (_pathStack.Peek()._y != pos._y)
            {
                _hintVerticalMatrix[Mathf.Min(_pathStack.Peek()._y, pos._y), _pathStack.Peek()._x].gameObject.SetActive(false);
            }
        }
    }
    /// <summary>
    /// Crea una pista para el usuario
    /// </summary>
    /// <param name="x">Posicion de la matriz de hints que se va a utilizar</param>
    /// <param name="y">Posicion de la matriz de hints que se va a utilizar</param>
    /// <param name="posX">Posicion en el eje X donde se va a colocar la imagen de ayuda</param>
    /// <param name="posY">Posicion en el eje Y donde se va a colocar la imagen de ayuda</param>
    private void CreateHint(int x, int y, float posX, float posY)
    {
        if (x + 1 < _boardWidth && _levelData._layout[y][x + 1] != '0')
        {
            GameObject hint = (GameObject)Instantiate(_hintGameObject, transform);
            hint.transform.parent = _hintsGroup;
            hint.GetComponent<Hint>().SetClueActive(false);
            hint.SetActive(false);

            float hintPosX = posX + 0.5f;
            hint.transform.localPosition = new Vector3(hintPosX, posY);

            _hintHorizontalMatrix[y, x] = hint.GetComponent<Hint>();
        }
        if (y + 1 < _boardHeight && _levelData._layout[y + 1][x] != '0')
        {
            GameObject hint = (GameObject)Instantiate(_hintGameObject, transform);
            hint.transform.parent = _hintsGroup;
            hint.GetComponent<Hint>().SetClueActive(false);
            hint.SetActive(false);

            float hintPosY = posY - 0.5f;
            hint.transform.localPosition = new Vector3(posX, hintPosY);
            hint.transform.Rotate(new Vector3(0, 0, 90));

            _hintVerticalMatrix[y, x] = hint.GetComponent<Hint>();
        }
    }
    /// <summary>
    /// Crea el camino solucion del tablero actual
    /// </summary>
    private void CreatePath()
    {
        for (int i = 0; i < _pathArray.Length - 1; i++)
        {
            List<int> path = _levelData._path[i];
            List<int> postPath = _levelData._path[i + 1];

            GameObject hint = null;

            if (path[1] != postPath[1])
            {
                hint = Instantiate(_hintHorizontalMatrix[path[0], Mathf.Min(path[1], postPath[1])].gameObject, transform);
                hint.transform.parent = _hintsGroup;
            }

            else if (path[0] != postPath[0])
            {
                hint = Instantiate(_hintVerticalMatrix[Mathf.Min(path[0], postPath[0]), path[1]].gameObject, transform);
                hint.transform.parent = _hintsGroup;
            }


            hint.GetComponent<Hint>().SetClueActive(true);
            _pathArray[i] = hint.GetComponent<Hint>();
        }
    }

    /// <summary>
    /// Realiza el escalado de la "malla" donde se colocará el tablero y lo posiciona segun el escalado
    /// </summary>
    private void ScaleGridAndSetPosition()
    {
        ScaleGrid();
        SetGridAtInitialPosition();
    }

    /// <summary>
    /// Centra el tablero en el espacio de coordenadas.
    /// </summary>
    private void SetGridAtInitialPosition()
    {
        _emptyCenterPosition = _canvasSize.GetEmptySpaceCenterPosition();
        Vector2 startPos = Camera.main.ScreenToWorldPoint(_emptyCenterPosition);

        startPos.y = -startPos.y;
        transform.position = startPos;

        float width = -_boardWidth / 2;
        if (_boardWidth % 2 == 0) width += 0.5f;

        float height = _boardHeight / 2;
        if (_boardHeight % 2 == 0) height -= 0.5f;
        transform.position = new Vector2(transform.position.x + (width * transform.localScale.x),
            transform.position.y + (height * transform.localScale.y));

    }


    /// <summary>
    /// Escala el tamaño del grid segun el tamaño de la pantalla.
    /// </summary>
    private void ScaleGrid()
    {
        _emptySpaceSize = _canvasSize.GetEmptySpaceSize();

        float tileWidth = _emptySpaceSize.x / _levelAvailableSpace[0];
        float tileHeight = _emptySpaceSize.y / _levelAvailableSpace[1];
        float tSize = Mathf.Min(tileWidth, tileHeight);
        _tileSize = new Vector2(tSize, tSize);

        Vector2 emptySize = new Vector2(_emptySpaceSize.x + Screen.width / 2,_emptySpaceSize.y + Screen.height / 2);
        emptySize = Camera.main.ScreenToWorldPoint(emptySize);

        float widthFactor = emptySize.x / _levelAvailableSpace[0];
        float heightFactor = emptySize.y / _levelAvailableSpace[1];
        float scaleFactor = Mathf.Min(widthFactor, heightFactor);

        transform.localScale = new Vector2(scaleFactor, scaleFactor);
    }

    /// <summary>
    /// Detecta la posicion del raton al ser pulsado y decide si el Tile se puede
    /// marcar como pulsado o no.
    /// </summary>
    private void InputController()
    {

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)

        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId == 0)
                {
                    if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
                        ProcessClickDown(touch.position);
                    else if (touch.phase == TouchPhase.Ended)
                        ProcessClickUp();
                }
            }
        }

#else

        if (Input.GetMouseButton(0))
        {
            ProcessClickDown(Input.mousePosition);

        }
        else if (Input.GetMouseButtonUp(0))
        {
            ProcessClickUp();
        }
#endif

    }

    private void Start()
    {
        PrepareLevel();
        StartCoroutine("StartScaleUpAnim");
    }

    private void Update()
    {
        if (!_levelFinished && !GameManager.Instance._challengeFailed)
            InputController();
        if (GameManager.Instance._challengeFailed)
        {
            _levelFinished = true;
            GameManager.Instance._challengeFailed = false;
        }

        if (GameManager.Instance.IsScreenSizeChanged())
        {
            ScaleGridAndSetPosition();
            GameManager.Instance.SetScreenSizeIsChanged(false);
        }
        Debug.Log(_hintCont);
    }

    /// <summary>
    /// Inicializa las matrices, arrays y pilas.
    /// </summary>
    private void InitializeMatrix()
    {
        _pressedTilesMatrix = new bool[_boardHeight, _boardWidth];

        _tilesMatrix = new Tile[_boardHeight, _boardWidth];

        _hintHorizontalMatrix = new Hint[_boardHeight, _boardWidth - 1];
        _hintVerticalMatrix = new Hint[_boardHeight - 1, _boardWidth];

        _pathArray = new Hint[_levelData._path.Count];

        _pathStack = new Stack<MatrixPos>();
    }

    /// <summary>
    /// Guarda el tamaño del tablero.
    /// </summary>
    /// <param name="gridSize">Tamaño del tablero</param>
    private void SetGridSize()
    {
        _boardWidth = _levelData._width;
        _boardHeight = _levelData._height;
    }

    /// <summary>
    /// Selecciona los colores por defecto y la piel del Tile y crea el tablero.
    /// </summary>
    /// <param name="categoryLevel">Categoria del nivel para crear</param>
    /// <param name="level">Nivel</param>
    private void CreateGrid()
    {
        SetBoardColor();
        GenerateGrid();
    }

    /// <summary>
    /// Asigna el color por defecto y una piel aleatoria al GameObject Tile.
    /// </summary>
    private void SetBoardColor()
    {
        _actualColor = UnityEngine.Random.Range(1, _resourceManager._blockScriptableObjects.Count);
        _cursorGameObject.GetComponent<SpriteRenderer>().sprite = _resourceManager._blockScriptableObjects[_actualColor].touch;

        if (_tileGameObject.TryGetComponent<Tile>(out Tile tile))
        {
            tile._defaultTile.sprite = _resourceManager._blockScriptableObjects[0].block;
            tile._colourTile.sprite = _resourceManager._blockScriptableObjects[_actualColor].block;
        }

        if (_hintGameObject.TryGetComponent<Hint>(out Hint hint))
        {
            hint._defaultTile.sprite = _resourceManager._blockScriptableObjects[0].hint;
            hint._colourTile.sprite = _resourceManager._blockScriptableObjects[_actualColor].hint;
        }
    }

    /// <summary>
    /// Crea Tiles y los asigna a la matriz de Tiles, ademas modifica la matriz de bool
    /// en la que todos son false excepto el Tile pulsado al inicio y los Tiles que no existen.
    /// Por ultimo, modifica la posicion del tablero para centrarlo en el espacio.
    /// </summary>
    /// <param name="layout">Tablero</param>
    private void GenerateGrid()
    {

        for (int y = 0; y < _boardHeight; y++)
        {
            for (int x = 0; x < _boardWidth; x++)
            {
                _pressedTilesMatrix[y, x] = true;
                if (_levelData._layout[y][x] != '0')
                {
                    GameObject tile = (GameObject)Instantiate(_tileGameObject, transform);

                    tile.transform.parent = _tilesGroup;
                    float posX = x;
                    float posY = -y;

                    posX = (float)Math.Round(posX, 0);
                    posY = (float)Math.Round(posY, 0);

                    tile.transform.localPosition = new Vector2(x, -y);

                    if (_levelData._layout[y][x] == '1')
                    {
                        _pressedTilesMatrix[y, x] = false;
                    }
                    else if (_levelData._layout[y][x] == '2')
                    {
                        _firsTile = new MatrixPos(x, y);
                        _pathStack.Push(new MatrixPos(x, y));
                    }

                    _tilesMatrix[y, x] = tile.GetComponent<Tile>();
                    _tilesMatrix[y, x].SetPressed(_pressedTilesMatrix[y, x]);

                    CreateHint(x, y, posX, posY);

                }
            }
        }

        ScaleGridAndSetPosition();
    }


}

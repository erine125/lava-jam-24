using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class Player : MonoBehaviour
{
    // Parameters \\

    public Sprite[] walkFrontSprites, walkSideSprites, walkBackSprites;

    public AudioSource audioSource; 
    public AudioClip dashSound; 

    public float walkSpeed;
    public float timePerFrame = 0.2f;
    public float dashMultiplier = 4f;
    public float dashLength = 3f;
    public float pickupDistance = 1.5f;
    public float coyoteTime = 0.25f;


    // State \\

    [Header("State")]
    public Facing facing;
    public Vector2 pos; // in tiles
    public State state;


    // Storage \\

    private SpriteRenderer spriteRenderer;
    private PantryParent parent;

    private float walkTimer;
    private int walkFrameIndex;

    private float dashDistanceLeft;

    public float coyoteTimeLeft;


    // Exposed \\

    void Awake ()
    {
        facing = Facing.DOWN;
        spriteRenderer = GetComponent<SpriteRenderer>();
        parent = GameObject.Find("PantryOnly").GetComponent<PantryParent>();
    }

    public void Init ()
    {
        pos = new Vector2(1, 8);
        state = State.STAND;
        transform.localScale = new Vector3(10, 10, 1);
        coyoteTimeLeft = 0;
    }

    void Update()
    {
        if (state == State.STAND || state == State.WALK)
        {
            PollArrowInput();
            PollDashInput();
        }
        if (state != State.DASHING && state != State.DYING)
        {
            parent.PlayerVulnerable();
        }

        CheckBoundsAndLeave();

        MovePlayer();
        transform.position = new Vector3(PantryParent.GridSquareSize * pos.x, PantryParent.GridSquareSize * pos.y, 0);
        UpdateSprite();
    }


    // Utility \\

    private void MovePlayer ()
    {
        float travel = Time.deltaTime * walkSpeed;
        travel *= state == State.DASHING ? dashMultiplier : 1;

        if (state == State.WALK || state == State.DASHING)
        {
            switch (facing)
            {
                case Facing.UP:
                    pos.y += travel;
                    break;
                case Facing.DOWN:
                    pos.y -= travel;
                    break;
                case Facing.LEFT:
                    pos.x -= travel;
                    break;
                case Facing.RIGHT:
                    pos.x += travel;
                    break;
            }

            if (state == State.DASHING)
            {
                dashDistanceLeft -= travel;

                if (dashDistanceLeft <= 0)
                {
                    state = State.STAND;
                }
            }
        }
    }

    private bool IsMoving ()
    {
        return state == State.WALK || state == State.DASHING;
    }

    private void PollArrowInput ()
    {
        bool up = Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.DownArrow);
        bool left = Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.RightArrow);

        if (up && !down && !left && !right)
        {
            facing = Facing.UP;
            state = State.WALK;
        }
        else if (!up && down && !left && !right)
        {
            facing = Facing.DOWN;
            state = State.WALK;
        }
        else if (!up && !down && left && !right)
        {
            facing = Facing.LEFT;
            state = State.WALK;
        }
        else if (!up && !down && !left && right)
        {
            facing = Facing.RIGHT;
            state = State.WALK;
        }
        else
        {
            state = State.STAND;
        }
    }

    private void PollDashInput ()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            dashDistanceLeft = dashLength;
            // play dash audio
            audioSource.PlayOneShot(dashSound, 0.5f); 
            state = State.DASHING;
        }
    }

    private void UpdateSprite ()
    {
        Sprite[] sprites = walkFrontSprites;
        bool invert = false;
        switch (facing)
        {
            case Facing.UP:
                sprites = walkBackSprites;
                break;
            case Facing.RIGHT:
                invert = true;
                sprites = walkSideSprites;
                break;
            case Facing.LEFT:
                sprites = walkSideSprites;
                break;
        }

        walkTimer += Time.deltaTime;
        if (walkTimer > timePerFrame && IsMoving ())
        {
            walkTimer = 0f;
            walkFrameIndex = (walkFrameIndex + 1) % 4;
            spriteRenderer.sprite = sprites[walkFrameIndex];
            spriteRenderer.flipX = invert;
        }
        else if (!IsMoving ())
        {
            spriteRenderer.sprite = sprites[0];
        }
    }

    private void CheckBoundsAndLeave ()
    {
        pos.x = Mathf.Clamp(pos.x, 0, 41);
        pos.y = Mathf.Clamp(pos.y, 0, 24);

        if (pos.x == 41 || pos.y == 0 || pos.y == 24)
        {
            state = State.STAND;
            coyoteTimeLeft = 0;
            parent.PlayerVulnerable();
        }
        else if (pos.x == 0 && facing == Facing.LEFT)
        {
            state = State.STAND;
            if (parent.manager.CountHeldIngredients () > 0)
            {
                parent.PlayerLeave();
            }
        }
    }


    // Structure \\

    public enum Facing
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    public enum State
    {
        STAND,
        WALK,
        DYING,
        DASHING,
        LEAVING
    }


}

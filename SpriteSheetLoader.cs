using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

//public enum Type { Persons, Things }
public enum Directions { Down, DownLeft, DownRight, Left, Right, Up, UpLeft, UpRight}

public class SpriteSheetLoader: MonoBehaviour
{
    //Spritesheet to render
    //public Texture newSpriteSheet;
    public string spriteSheetPath;
    private string fullSpriteSheetPath;
    private char[] filePathDelim = { '/', '.' };
    private SpriteRenderer spriteRenderer;
    private string nextFrame;
    private int currentFrameNumber;
    private int firstFrameNumber = 1;

    // Camera
    private CameraController mainCamera;

    //Type to set filepath for spritesheet
    //private Type type;
    private Directions direction;
    private Animator directionController;
    private Dictionary<string, Sprite> spriteSheet;    

    // List of available animations
    public Dictionary<string, SpriteSheetAnim> allAnimations;

    // Curently playing animations
    public SpriteSheetAnim currentAnimation;
    private bool isInAnimation = false;

    //Animation to default to
    public string defaultAnimation = "Idle";

    //Slow down animation
    float animSpeedMod = 1;
    bool speedChange = true;
    int counter = 0;
    int targetFPS = 0;
    int counterMax = 0;



    // Start is called before the first frame update
    void Start()
    {
        // Init dictionary of animations
        allAnimations = new Dictionary<string, SpriteSheetAnim>();
        if (allAnimations == null)
        {
            Debug.LogError("Failed to init animation");

        }

        // Get the sprite renderer for the object to animate
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(!spriteRenderer)
        { 
            Debug.LogError("No sprite renderer");
        }

        // Set up camera and animation speed
        this.mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraController>();
        if(!mainCamera)
        {
            Debug.LogError("no camera controller found");
        }

        // Get DirectionController
        this.directionController = GetComponent<Animator>();
        if(!directionController)
        {
            Debug.LogError("no direction control");
        }

        SetDirection(Directions.Down);

        LoadSpriteSheet(spriteSheetPath);

        //Render First frame
        currentAnimation = allAnimations[defaultAnimation];
        currentFrameNumber = firstFrameNumber;
        SetNextFrame();
        RenderFrame();

        
    }



    // Update is called once per frame
    void LateUpdate()
    {
        //Set the next frame and render it
        SetNextFrame();
        RenderFrame();
    }



    //Render the next frame
    private void RenderFrame()
    {
        spriteRenderer.sprite = spriteSheet[nextFrame];
    }


    //Set the next frame to render
    private void SetNextFrame()
    {
        // If not on the last frame of an animation
        if(currentFrameNumber < currentAnimation.GetNumberOfFrames())
        {
            IncrementFrameCount();
        }
        else
        {
            // Reset frame number
            currentFrameNumber = firstFrameNumber;
            this.isInAnimation = false;
        }
       
        nextFrame = currentAnimation.GetAnimationName() + "-" + direction.ToString() + "-" + currentFrameNumber;
    }

    //Increment the frame number based on animationspeed
    private void IncrementFrameCount()
    {
        if(speedChange)
        {
            targetFPS = mainCamera.targetFramesPerSecond;

            // Speed mod of 0 not allowed
            if(this.animSpeedMod == 0)
            {
                this.animSpeedMod = 1;
            }

            // If > 0 increase animation speed
            if(this.animSpeedMod > 0)
            {
                counterMax = Mathf.RoundToInt(targetFPS / animSpeedMod);
            }
            // If < 0 slow down animation speed
            else
            {
                counterMax = Mathf.RoundToInt(Mathf.Abs(animSpeedMod) * targetFPS);
            }

            speedChange = false;
        }
      

        if(counter < counterMax)
        {
            counter++;
        }
        else
        {
            currentFrameNumber++;
            counter = 0;
        }
    }

    [SerializeField]
    // Loads all sprites from the sprite sheet
    // Spritesheet must be in the Resources folder
    public void LoadSpriteSheet(string path)
    {

        var sprites = Resources.LoadAll<Sprite>(path);
        spriteSheet = sprites.ToDictionary(x => x.name, x => x);

        LoadActions();
    }



    // Load animations from sprite sheet and save them as Actions
    private void LoadActions()
    {
        List <string[]> tempActions = new List<string[]>();

        foreach(KeyValuePair<string, Sprite> sprite in spriteSheet)
        {
            tempActions.Add(sprite.Key.Split('-'));
        }

        foreach(string[] temp in tempActions)
        {
            // Add new action
            if(!allAnimations.ContainsKey(temp[0]))
            {
                SpriteSheetAnim newAnimation = new SpriteSheetAnim(temp[0], firstFrameNumber);
                allAnimations.Add(temp[0], newAnimation);
                //Debug.Log("added action: " + temp[0]);
            }
            // Increase frame count of existing action
            else
            {
                int frameNumber;
                int.TryParse(temp[temp.Length - 1], out frameNumber);

                if(allAnimations[temp[0]].GetNumberOfFrames() < frameNumber)
                {
                    allAnimations[temp[0]].AddFrame();
                }     
            }
        }
    }


    // Retuen the current action in use
    public SpriteSheetAnim GetCurrentAction()
    {
        return this.currentAnimation;
    }


    // Set direction of action
    public void SetDirection(Directions newDirection)
    {
        this.direction = newDirection;
    }


    // Set direction of animation by movement
    // Uses blend tree 2d cart movement
    public void SetDirection(Vector2 movement)
    {
        if(movement.magnitude > 0)
        {
            directionController.SetFloat("xDir", movement.x);
            directionController.SetFloat("yDir", movement.y);
        }

        AnimatorClipInfo[] dirInfo;
        dirInfo = this.directionController.GetCurrentAnimatorClipInfo(0);

        switch (dirInfo[0].clip.name)
        {
            case "UL":
                SetDirection(Directions.UpLeft);
                break;
            case "U":
                SetDirection(Directions.Up);
                break;
            case "UR":
                SetDirection(Directions.UpRight);
                break;
            case "R":
                SetDirection(Directions.Right);
                break;
            case "DR":
                SetDirection(Directions.DownRight);
                break;
            case "D":
                SetDirection(Directions.Down);
                break;
            case "DL":
                SetDirection(Directions.DownLeft);
                break;
            case "L":
                SetDirection(Directions.Left);
                break;
            default:
                break;
        }
    }

    // Transition current animation to new one without resetting frames
    private void PlayAnimation(SpriteSheetAnim anim, float speed = 0)
    {
        // Set current animation to new animation
        this.currentAnimation = anim;

        // If new animation is shorter than old animation
        if(currentAnimation.GetNumberOfFrames() <= currentFrameNumber)
        {
            // Reset the animation
            currentFrameNumber = firstFrameNumber;
        }

        SetAnimSpeedModifier(speed);
    }

    public void PlayAnimation(string anim, float speed = 0)
    {
        if(allAnimations.ContainsKey(anim))
        {
            PlayAnimation(allAnimations[anim], speed);
        }
        else
        {
            Debug.LogError(anim + " is not a valid animation.");
        }
    }

    // Sets the current animation to a new animation
    private void PlayNewAnimation(SpriteSheetAnim anim, float speed = 0)
    {
        if(currentAnimation != anim)
        {
            currentAnimation = anim;
            currentFrameNumber = firstFrameNumber;
        }

        SetAnimSpeedModifier(speed);
    }

    public void PlayNewAnimation(string anim, float speed = 0)
    {
        if (allAnimations.ContainsKey(anim))
        {
            PlayNewAnimation(allAnimations[anim], speed);
        }
        else
        {
            Debug.LogError(anim + " is not a valid animation.");
        }
    }

    // Sets isInActon to true, resets to false at end of current animation
    public void LockAnimation()
    {
        this.isInAnimation = true;
    }

    public bool IsInAnimation()
    {
        return this.isInAnimation;
    }

    //Set the animation speed modifier
    public void SetAnimSpeedModifier(float newSpeedMod)
    {
        if(newSpeedMod != 0 && newSpeedMod != this.animSpeedMod)
        {
            this.animSpeedMod = newSpeedMod;
            speedChange = true;
        }
    }

    public bool HasAction(string actionName)
    {
        return allAnimations.ContainsKey(actionName);
    }
}


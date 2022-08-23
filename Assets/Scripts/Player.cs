using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    public string username;
    public bool team;
    public CharacterController controller;
    public Transform shootOrigin;
    public float gravity = -9.81f;
    private float moveSpeed = 100f;
    public float jumpSpeed = 5f;
    public float health;
    public float maxHealth= 100f;
    private List<Vector3> positons;



    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;


    private bool[] inputs;
    private float yVelocity;
    private void Start()
    {
        // gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        //tak bylo  moveSpeed *= Time.fixedDeltaTime;
        moveSpeed = 2;
        //jumpSpeed *= Time.fixedDeltaTime;
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;
        Debug.Log("MIN TIME" + minTimeBetweenTicks);
        positons = new List<Vector3> { };
    }

    public void Initialize(int _id, string _username,bool _team)
    {
        id = _id;
        username = _username;
        team = _team;
        health = maxHealth;

        inputs = new bool[5];
    }

    /// <summary>Processes player input and moves the player.</summary>
    public void Update()
    {

        timer += Time.deltaTime;

        while (timer >= minTimeBetweenTicks)
        {
            currentTick = currentTick % BUFFER_SIZE;
            timer -= minTimeBetweenTicks;
            
            if (positons.Count == BUFFER_SIZE)
            {
                positons = new List<Vector3> { };
            }
           // Debug.Log(gameObject.transform.position+"----"+currentTick);
            positons.Add(gameObject.transform.position);
            
            



            if (health <= 0f)
            {
                return;
            }
            Vector2 _inputDirection = Vector2.zero;
            if (inputs[0])
            {
                _inputDirection.y += 1;
            }
            if (inputs[1])
            {
                _inputDirection.y -= 1;
            }
            if (inputs[2])
            {
                _inputDirection.x -= 1;
            }
            if (inputs[3])
            {
                _inputDirection.x += 1;
            }

            Move(_inputDirection,currentTick,positons[currentTick]);
           // Debug.Log(currentTick);
            currentTick++;
        }


       
    }

    /// <summary>Calculates the player's desired movement direction and moves him.</summary>
    /// <param name="_inputDirection"></param>
    private void Move(Vector2 _inputDirection,int tick, Vector3 position)
    {
        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        _moveDirection *= moveSpeed;
        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (inputs[4])
            {
                yVelocity = jumpSpeed;
            }
        }

        /*
         * tak bylo
          yVelocity += gravity*Time.deltaTime;
       

        _moveDirection.y = yVelocity;
        controller.Move(_moveDirection*Time.deltaTime);
         */


        yVelocity += gravity* minTimeBetweenTicks;
       

        _moveDirection.y = yVelocity;
       // Debug.Log(currentTick + " " + _moveDirection * minTimeBetweenTicks);
        controller.Move(_moveDirection* minTimeBetweenTicks);

       // Debug.Log(tick);
        ServerSend.CheckPlayer(id,position,tick);
                ServerSend.PlayerPosition(this);                  //WAŻNE
                ServerSend.PlayerRotation(this);
                ServerSend.CamRotation(this);
                
            

        


    }

    /// <summary>Updates the player input with newly received input.</summary>
    /// <param name="_inputs">The new key inputs.</param>
    /// <param name="_rotation">The new rotation.</param>
    public void SetInput(bool[] _inputs, Quaternion _rotation, Quaternion _Camrotation)
    {

        inputs = _inputs;
        transform.rotation = _rotation;
        transform.GetChild(2).transform.rotation = _Camrotation;
    }
    public void Shoot(Vector3 _yViewDirection)
    {
       
        if (Physics.Raycast(shootOrigin.position,_yViewDirection,out RaycastHit _hit,25f))
        {
            if (_hit.collider.CompareTag("Player"))
            {
                _hit.collider.GetComponent<Player>().TakeDamage(50f);
            }
        }
    }
    public void TakeDamage(float _damage)
    {
        if (health<=0f)
        {
            return;
        }
        health -= _damage;
        if (health<=0f)
        {
            health = 0f;
            controller.enabled = false;
            transform.position = new Vector3(0f, 25f, 0f);
            ServerSend.PlayerPosition(this);
            StartCoroutine(Respawn());
        }
        ServerSend.PlayerHealth(this);
    }
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5f);

        health = maxHealth;
        controller.enabled = true;
        ServerSend.PlayerRespawned(this);
    }

}

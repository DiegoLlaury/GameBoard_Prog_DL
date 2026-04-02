using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.AI;

public enum StateType
{
    None,
    Patrol,
    Follow,
    Attack
}

public class ZombieIAController : MonoBehaviour
{
    [SerializeField] private StateType state = StateType.None;
    [SerializeField] private StateType nextState = StateType.None;
    [SerializeField] private GameObject target;
    [SerializeField] private GameObject navpoint;
    [SerializeField] private float attackDistance = 1.5f; 


    // Update is called once per frame
    void Update()
    {
        if (TestChangeState())
        {

            ChangeState();
        }
        BehaviourAction();
    }

    private bool TestChangeState()
    {
        switch (state)
        {
            case StateType.Follow:

                if (Vector3.Distance(target.transform.position, transform.position) <= attackDistance)
                {
                    nextState = StateType.Attack;
                    return true;
                }
                break;
        }
        return false;
    }

    private void ChangeState()
    {
        EndState();
        state = nextState;
        StartState();
    }

    private void StartState()
    {

        switch (state)
        {
            case StateType.Follow:
                
                break;
        }

    }

    private void EndState()
    {
        switch (state)
        {
            case StateType.Follow:
                GetComponent<NavMeshAgent>().SetDestination(transform.position);
                break;
        }
    }

    //Réaliser le comportement actuel
    private void BehaviourAction()
    {

        switch (state)
        {
            case StateType.Patrol:
                PatrolBehaviour();
                break;

            case StateType.Follow:
                FollowBehaviour();
                break;

            case StateType.Attack:
                AttackBehaviour();
                break;
        }
    }
    
    private void PatrolBehaviour()
    {
        GetComponent<NavMeshAgent>().SetDestination(navpoint.transform.position);
    }

    private void FollowBehaviour()
    {
        GetComponent<NavMeshAgent>().SetDestination(target.transform.position);
    }

    private void AttackBehaviour()
    {
        GetComponent<Animator>().SetTrigger(name:"Attack");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class MovingPlatform : MonoBehaviour
{
    [SerializeField] private List<Transform> nodes;

    private Vector3 CurrentNode => nodes[currentNodeIndex % nodes.Count].position;
    private int currentNodeIndex;
    [SerializeField] protected float speed = .001f;
    [SerializeField] protected float pauseDuration;

    private IEnumerator Start()
    {
        for (; ; )
        {
            transform.position = Vector3.MoveTowards(transform.position, CurrentNode, speed);
            if (transform.position == CurrentNode)
            {
                currentNodeIndex++;
                yield return new WaitForSeconds(pauseDuration);
            }
            yield return null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.parent = transform;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.parent = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var node in nodes)
        {
            Gizmos.DrawWireCube(node.position, new(.3f, .3f, .3f));
        }
    }

}

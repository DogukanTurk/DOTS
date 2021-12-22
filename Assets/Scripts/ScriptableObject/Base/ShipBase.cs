using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/Enemy Data")]
public class ShipBase : ScriptableObject
{

    /* ------------------------------------------ */

    public int Team;

    public int Health;

    public int GunRange;
    public int GunDamage;

    public int MoveSpeed;

    public Mesh Mesh;

    public Material MatRed, MatBlue;

    /* ------------------------------------------ */

}
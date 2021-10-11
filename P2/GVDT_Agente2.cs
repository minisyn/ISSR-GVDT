using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Hola don pepito
//hola don jose
public class GVDT_Agente2 : ISSR_Agent
{
    public bool target_fixed = false;
    // Start is called before the first frame update
    public override void Start()
    {
        //  Debug.LogFormat("{0}: Comienza", Myself.Name);
    }

    // Update is called once per frame
    public override IEnumerator Update()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            //En este punto generaremos un evento de tiempo
            //current_event = ISSREventType.onTickElapsed;
        }
    }
    ISSRState AgentStateMachine() // Función principal de máquina de estados
    {
        ISSRState next_state = current_state; // estado de salida, en principio igual


        switch (current_state) // Según el estado
        {

            default:
                Debug.LogErrorFormat("{0}: estado {1} no considerado", Myself.Name,
                    current_state);
                break;
        }

        if (current_state != next_state) // Si ha cambiado el estado
        {
            Debug.LogWarningFormat("{0}: Estado '{1}'-->'{2}' por evento '{3}'", Myself.Name,
                           current_state, next_state, current_event);
        }

        return next_state;
    }

    // Código vacío para función de estado Idle y OTROS DOS
    ISSRState SF_Idle()  // SF “State Function”
    {
        ISSRState next_state = current_state;

        switch (current_event) // Según el evento
        {
            default:
                // Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",                     
                //    Myself.Name, current_event, current_state);
                break;
        }

        return next_state;
    }

    ISSRState SF_GoinToGripSmallStone()
    {
        ISSRState next_state = current_state;
        switch (current_event)
        {
            default:
                break;
        }
    }

    ISSRState SF_GoingToGoalWithSmallStone()
    {
        ISSRState next_state = current_state;
        switch (current_event)
        {
            default:
                break;
        }
    }


    public override void onEnterSensingArea(ISSR_Object obj)
    {
        objet_just_seen = obj;

        if ((obj.type == ISSR_Type.SmallStone) && !Valid_Small_Stones.Contains((obj)))
        {
            //target_fixed = true;
            Valid_Small_Stonees.add(obj);
            Debug.LogFormat("{0}: nueva piedra pequeña {1}, anotada", Myself.Name, obj.Name);
        }
    }

    //Podemos programar las acciones
    public override void onGripSuccess(ISSR_Object obj_gripped)
    {

        Debug.LogFormat("{0}: piedra {1} cogida, voy para la meta", Myself.Name, obj_gripped.Name);
        acGotoLocation(iMyGoalLocation());
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored)
    {
        //target_fixed = false;
        Debug.LogFormat("{0}: piedra {1} en meta", Myself.Name, stone_that_scored.Name);
    }
}

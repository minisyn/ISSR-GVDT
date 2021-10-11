using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GVDT_Agente2 : ISSR_Agent
{
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
            current_event = ISSREventType.onTickElapsed;
            current_state = AgentStateMachine();
        }
    }
       // Código vacío para función de estado Idle y OTROS DOS
    ISSRState SF_Idle()  // SF “State Function”
    {
        ISSRState next_state = current_state;

        switch (current_event) // Según el evento
        {
            case ISSREventType.onTickElapsed:
                //coger de la lista el objeto más cercano al agente del tipo SmallStone (Objeto focuseado)
                focus_object = ISSRHelp.GetCloserToMeObjectInList(this, Valid_Small_Stones, ISSR_Type.SmallStone);

                if (focus_object != null)
                {
                    Debug.LogFormat("{0}: trata de coger {1}", Myself.Name, focus_object.Name);
                    acGripObject(focus_object);
                    if (acCheckError())
                    {
                        next_state = ISSRState.Error;
                    }
                    else
                    {
                        next_state = ISSRState.GoingToGripSmallStone;
                    }
                }
                else
                {
                    Debug.LogFormat("{0}: no reconozco más piedras pequeñas", Myself.Name);
                    next_state = ISSRState.End;
                }
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
            case ISSREventType.onGripSuccess:
                Valid_Small_Stones.Remove(focus_object);
                acGotoLocation();
                if (acCheckError)
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    Debug.Debug.LogFormat("{0}: De camino a la meta", Myself.Name);
                    next_state = ISSRState.GoingToGoal;
                }
                break;
            default:
                break;
        }
    }

    ISSRState SF_GoingToGoalWithSmallStone()
    {
        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onGObjectScored:
                next_state = ISSRState.Idle;
                Debug.LogFormat("{0}: Piedra {1} anotada", Myself.Name, focus_object.Name);
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
        current_state = AgentStateMachine();
    }

    //Podemos programar las acciones
    public override void onGripSuccess(ISSR_Object obj_gripped)
    {

        Debug.LogFormat("{0}: agarra {1}, va para la meta", Myself.Name, obj_gripped.Name);
        current_state = AgentStateMachine();
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored)
    {
        //target_fixed = false;
        Debug.LogFormat("{0}: piedra {1} en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine();
    }
 ISSRState AgentStateMachine() // Función principal de máquina de estados
    {
        ISSRState next_state = current_state; // estado de salida, en principio igual

        switch (current_state) // Según el estado
        {
            case ISSRState.Idle:
                next_state = SF_Idle();
                break;
            case ISSRState.GoingToGripSmallStone:
                next_state = SF_GoingToGripSmallStone();
                break;
            case ISSRState.GoingToGoalWithSmallStone:
                next_state = SF_GoingToGoalWithSmallStone();
                break;
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
}
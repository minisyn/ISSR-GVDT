using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Agente2 : ISSR_Agent {

	public override void Start()
    {
        Debug.LogFormat("{0}: comienza", Myself.Name);

    }

    public override  IEnumerator Update()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            current_event = ISSREventType.onTickElapsed;
            current_state = AgentStateMachine();
        }

    }

    ISSRState AgentStateMachine()//Función principal de máquina de estados
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_state)

        {
            case ISSRState.Idle:
                next_state = SF_Idle();
                break;

            case ISSRState.GoingToGripSmallStone:

                next_state = SF_GoingToGripSmallStone();
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                next_state =SF_GoingToGoalWithSmallStone();
                break;

            case ISSRState.End:
                break;
            case ISSRState.Error:
                break;

            default:

                Debug.LogFormat("{0}: Estado '{1}' no considerado", Myself.Name, current_state);
                break;

        }
        if (current_state != next_state) //si ha cambiado el estado
        {
            Debug.LogWarningFormat("{0}: Estado '{1}'-->'{2}' por evento '{3}'",Myself.Name,current_state,next_state,current_event);
        }

        return next_state;

    }

    //FUNCIONES PARA CADA ESTADO
    ISSRState SF_Idle()//SF--> "State Function"
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onTickElapsed:

                focus_object = ISSRHelp.GetCloserToMeObjectInList(this, Valid_Small_Stones, ISSR_Type.SmallStone);///coge de la lista la piedra pequeña más cercana al agente y la convierte en objeto de interés
                if (focus_object != null)//si hay alguna piedra pequeña 
                {
                    next_state = ProcessStone();
                }
                else//Si focus_object es null significa que no hay más piedras disponibles
                {
                    Debug.LogFormat("{0}: No conozco más piedras pequeñas", Myself.Name);

                }

                break;
            default:

                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_event);
                break;

        }
       
        return next_state;

    }

    private ISSRState ProcessStone()
    {
        ISSRState next_state;
        Debug.LogFormat("{0}: Trata de coger '{1}'", Myself.Name, focus_object.Name);

        if (oiSensable(focus_object))//si está a la vista va a coger el objeto
        {
            acGripObject(focus_object);

        }
        else //sino está a la vista irá a la última posición donde lo vio
        {
            acGotoLocation(focus_object.LastLocation);
        }

        if (acCheckError())//si hay error en la acción
        {
            next_state = ISSRState.Error;//señala error
        }
        else//si no hay error
        {
            next_state = ISSRState.GoingToGripSmallStone;//cambia a estado de ir a coger piedra pequeña
        }

        return next_state;
    }

    ISSRState SF_GoingToGripSmallStone()//SF--> "State Function"
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onGripSuccess:

                Valid_Small_Stones.Remove(focus_object);//Elimina el objeto de interés de la lista de piedras pequeñas disponibles
                acGotoLocation(iMyGoalLocation());
                if(acCheckError())
                {
                    next_state = ISSRState.Error;//señala error
                }
                else
                {
                    next_state = ISSRState.GoingToGoalWithSmallStone;
                    Debug.LogFormat("{0}: Piedra '{1}' agarrada con éxito", Myself.Name, focus_object.Name);

                }
                break;

            case ISSREventType.onEnterSensingArea:
                if (object_just_seen.Equals(focus_object))
                {
                    acGripObject(focus_object);

                    if (acCheckError())
                    {
                         next_state = ISSRState.Error;//señala error
                    }
                    
                }
          
                break;

            default:

                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_event);
                break;

        }

        return next_state;

    }

    ISSRState SF_GoingToGoalWithSmallStone()//SF--> "State Function"
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onGObjectScored:

                next_state = ISSRState.Idle;//Pasa a estado inactivo
                Debug.LogFormat("{0}: Piedra '{1}' introducida con éxito", Myself.Name, focus_object.Name);
                break;

            default:

                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_event);
                break;
        }

        return next_state;

    }

    //FUNCIONES DE EVENTOS

    public override void onEnterSensingArea(ISSR_Object obj)
    {
        object_just_seen = obj;//Anotams objeto que vemos

        if (obj.type == ISSR_Type.SmallStone)
        {
            if (!Valid_Small_Stones.Contains(obj))
            {
                Valid_Small_Stones.Add(obj);
                Debug.LogFormat("{0}: nueva piedra pequeña '{1}' anotada", Myself.Name, obj.Name);
            }
           
        }
        current_state = AgentStateMachine();
    }

    public override void onGripSuccess(ISSR_Object obj_gripped)
    {
        Debug.LogFormat("{0}: piedra '{1}' agarrada ", Myself.Name, obj_gripped.Name);
        current_state = AgentStateMachine();
       
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored)
    {
        Debug.LogFormat("{0}: piedra '{1}' metida en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine();
    }



}

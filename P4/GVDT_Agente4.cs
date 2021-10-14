using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//REALIZAR UNA FUNCION PARA LA COMPROBACIÓN DE ERRORES DE FORMA AUTOMÁTICA (JUGAR CON HERENCIAS)

public class GVDT_Agente4 : ISSR_Agent
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
                    next_state = processStone();
                }
                else
                {
                    Debug.LogFormat("{0}: no reconozco más piedras pequeñas", Myself.Name);
                    next_state = ISSRState.End;
                }
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                   Myself.Name, current_event, current_state);
                break;
        }

        return next_state;
    }

    ISSRState SF_GoingToGripSmallStone()
    {

        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onGripSuccess:

                Valid_Small_Stones.Remove(focus_object);
                acGotoLocation(iMyGoalLocation());
                if (acCheckError())
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    Debug.LogFormat("{0}: De camino a la meta", Myself.Name);
                    next_state = ISSRState.GoingToGoalWithSmallStone;
                }
                break;

            case ISSREventType.onEnterSensingArea:

                if (object_just_seen.Equals(focus_object)) // veo justo la piedra que 'recuerdo'
                {
                    acGripObject(focus_object); // intento agarrar esa piedra pequeña
                    if (acCheckError()) // si hay error en la acción:
                    {
                        next_state = ISSRState.Error; // Señala Error
                    }
                }
                break;

            case ISSREventType.onCollision:

                if (colliding_object.Equals(focus_object))
                {
                    next_state = processStone();
                    if (acCheckError())
                    {
                        next_state = ISSRState.Error;
                    }
                }
                else
                {
                    next_state = processCollision();
                }
                break;

            default:

                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                      Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    ISSRState SF_GoingToGoalWithSmallStone()
    {

        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onGObjectScored:

                next_state = ISSRState.Idle;
                Debug.LogFormat("{0}: Piedra {1} anotada", Myself.Name, focus_object.Name);
                break;

            case ISSREventType.onCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onGObjectCollision:

                next_state = processCollision();
                break;

            default:

                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                      Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    ISSRState SF_AvoidingObstacle()
    {

        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onGObjectCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onDestArrived:

                next_state = ResumeAfterCollision();
                break;

            case ISSREventType.onGObjectScored:

                next_state = SF_Idle();
                break;

            default:

                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                      Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState processStone()
    {
        ISSRState next_state;

        if (oiSensable(focus_object))
        {
            Debug.LogFormat("{0}: trata de coger {1}", Myself.Name, focus_object.Name);
            acGripObject(focus_object);

        }
        else
        {
            Debug.LogFormat("{0}: {1} fuera de vista voy a su última posición", Myself.Name, focus_object.Name);
            acGotoLocation(oiLastLocation(focus_object));
        }
        if (acCheckError())
        {
            next_state = ISSRState.Error;
        }
        else
        {
            next_state = ISSRState.GoingToGripSmallStone;
        }

        return next_state;
    }


    private ISSRState processCollision()
    {
        ISSRState next_state = current_state;

        switch (current_state)
        {
            case ISSRState.GoingToGripSmallStone:

                last_state = current_state;
                next_state = acGotoSafeLocation();
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                last_state = current_state;
                next_state = acGotoSafeLocation();
                break;

            case ISSRState.AvoidingObstacle:

                next_state = acGotoSafeLocation();
                break;

            default:

                Debug.LogErrorFormat("{0}, estado {1} no considerado al colisionar", Myself.Name, current_state);
                break;
        }

        return next_state;
    }

    private ISSRState acGotoSafeLocation()
    {

        ISSRState next_state;
        acGotoLocation(ISSRHelp.CalculateSafeLocation(this, colliding_object));
        if (acCheckError())
        {
            next_state = ISSRState.Error;
        }
        else
        {
            next_state = ISSRState.AvoidingObstacle;
        }
        return next_state;
    }

    ISSRState ResumeAfterCollision() // Versi�n de Pr�ctica 3, completar en las siguientes
    { // Continuar con lo que se estaba haciendo en el momento de la colisi�n.
        ISSRState next_state = current_state;

        switch (last_state)
        {
            case ISSRState.GoingToGripSmallStone:

                next_state = processStone();  // Volver a pedir coger piedra o ir a su lugar
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                acGotoLocation(iMyGoalLocation());  // volver a pedir ir a la meta
                if (acCheckError())
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    next_state = ISSRState.GoingToGoalWithSmallStone;
                }
                break;

            default:
                Debug.LogErrorFormat("{0}, estado {1} no considerado al volver de colisi�n", Myself.Name, last_state);
                break;
        }
        return next_state;
    }

    public override void onEnterSensingArea(ISSR_Object obj)
    {
        object_just_seen = obj;

        if ((obj.type == ISSR_Type.SmallStone) && !Valid_Small_Stones.Contains((obj)))
        {
            Valid_Small_Stones.Add(obj);
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
        Debug.LogFormat("{0}: piedra {1} en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine();
    }

    public override void onCollision(ISSR_Object obj_that_collided_with_me)
    {
        //Objeto contra el propio agente
        colliding_object = obj_that_collided_with_me;
        current_state = AgentStateMachine();
    }

    public override void onGObjectCollision(ISSR_Object obj_that_collided_with_gripped_obj)
    {
        //Objeto contra el objeto agarrado
        colliding_object = obj_that_collided_with_gripped_obj;
        current_state = AgentStateMachine();
    }

    public override void onDestArrived()
    {
        current_state = ResumeAfterCollision();
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

            case ISSRState.WaitforNoStonesMoving:

                break;

            case ISSRState.AvoidingObstacle:

                next_state = SF_AvoidingObstacle();
                break;

            case ISSRState.End:

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